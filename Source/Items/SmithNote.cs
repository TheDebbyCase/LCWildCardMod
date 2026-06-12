using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using GameNetcodeStuff;
using TMPro;
using System.Linq;
using UnityEngine.UI;
using System.Collections;
using LCWildCardMod.Utils;
using Steamworks;
using System;
namespace LCWildCardMod.Items
{
    public class SmithNote : WildCardProp
    {
        internal static readonly Dictionary<string, Texture2D> enemyPageTextures = new Dictionary<string, Texture2D>();
        private static Animator localHudAnim = null;
        private static int noteCount = 0;
        private static int pageMask = 0;
        [Space(3f)]
        [Header("SmithNote")]
        [Space(3f)]
        [SerializeField]
        private Transform hudTextures = null;
        [SerializeField]
        private List<TextMeshProUGUI> textMeshList = null;
        [SerializeField]
        private List<RawImage> rawImageList = null;
        [SerializeField]
        private List<SelectablePair<Texture2D>> enemyTextures = new List<SelectablePair<Texture2D>>();
        [SerializeField]
        private Texture2D defaultPlayerTexture = null;
        [SerializeField]
        private Texture2D defaultEnemyTexture = null;
        [SerializeField]
        private float addPageDistance = 10f;
        private readonly ListDict<int, SmithNoteInfo> playerPages = new ListDict<int, SmithNoteInfo>();
        private readonly ListDict<int, SmithNoteInfo> enemyPages = new ListDict<int, SmithNoteInfo>();
        private readonly Dictionary<int, EnemyAI> enemyIndices = new Dictionary<int, EnemyAI>();
        private ListDict<int, SmithNoteInfo> allPages = new ListDict<int, SmithNoteInfo>();
        private int currentElement = 0;
        private bool currentlyEnemy = false;
        private float selectCooldown = 0;
        private bool isFlippable = false;
        private PlayerControllerB killingPlayer = null;
        private EnemyAI killingEnemy = null;
        private int lastLivingPlayers = 0;
        private float lastEnemyPower = 0f;
        private Coroutine killCoroutine = null;
        private ListDict<int, SmithNoteInfo> AllPages
        {
            get
            {
                if (playerPages.Count + enemyPages.Count != allPages.Count)
                {
                    allPages = playerPages.Combined(enemyPages);
                }
                return allPages;
            }
        }
        private float TotalEnemyPower
        {
            get
            {
                RoundManager rm = RoundManager.Instance;
                return rm.currentEnemyPower + rm.currentOutsideEnemyPower + rm.currentDaytimeEnemyPower + rm.currentWeedEnemyPower;
            }
        }
        public override void Start()
        {
            base.Start();
            noteCount++;
            textMeshList[2].rectTransform.parent.gameObject.SetActive(true);
            lastLivingPlayers = StartOfRound.Instance.livingPlayers;
            lastEnemyPower = TotalEnemyPower;
        }
        internal override void InitializePrefab()
        {
            base.InitializePrefab();
            pageMask = LayerMask.GetMask("Player", "Enemies");
            if (enemyTextures != null)
            {
                enemyPageTextures.Clear();
                for (int i = 0; i < enemyTextures.Count; i++)
                {
                    SelectablePair<Texture2D> pair = enemyTextures[i];
                    enemyPageTextures.Add(pair.id, pair.selectable);
                }
                enemyTextures = null;
            }
            if (SmithNoteInfo.playerDefault != null && SmithNoteInfo.enemyDefault != null)
            {
                return;
            }
            SmithNoteInfo.playerDefault = defaultPlayerTexture;
            SmithNoteInfo.enemyDefault = defaultEnemyTexture;
            defaultPlayerTexture = null;
            defaultEnemyTexture = null;
        }
        public override void OnDestroy()
        {
            noteCount--;
            if (noteCount <= 0 && localHudAnim != null)
            {
                Destroy(localHudAnim.gameObject);
                localHudAnim = null;
            }
            base.OnDestroy();
        }
        public override void EquipItem()
        {
            base.EquipItem();
            SetHUD();
            int heldID = (int)LastPlayerHeldBy.playerClientId;
            if (IsOwner && !playerPages.ContainsKey(heldID))
            {
                NewPlayer(heldID);
            }
            textMeshList[2].rectTransform.parent.gameObject.SetActive(true);
            StartOpening();
        }
        internal override void GrabFromAny(bool fromPlayer = true, EnemyAI enemy = null)
        {
            base.GrabFromAny(fromPlayer, enemy);
            SetHUD();
            textMeshList[2].rectTransform.parent.gameObject.SetActive(true);
            StartOpening();
        }
        public override void GrabItemFromEnemy(EnemyAI enemy)
        {
            if (IsOwner && !enemyPages.ContainsKey(enemy.thisEnemyIndex))
            {
                NewEnemy(enemy);
            }
            base.GrabItemFromEnemy(enemy);
        }
        public override void PocketItem()
        {
            base.PocketItem();
            StartClosing();
            textMeshList[0].rectTransform.parent.gameObject.SetActive(false);
            rawImageList[0].rectTransform.parent.gameObject.SetActive(false);
            textMeshList[2].rectTransform.parent.gameObject.SetActive(false);
        }
        internal override void DiscardFromAny(bool fromPlayer = true, EnemyAI enemy = null)
        {
            base.DiscardFromAny(fromPlayer, enemy);
            StartClosing();
        }
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            if (!isFlippable)
            {
                return;
            }
            StartFlipping();
            base.ItemActivate(used, buttonDown);
        }
        public override void Update()
        {
            base.Update();
            if ((killingPlayer != null && killingPlayer.isPlayerDead) || (killingEnemy != null && killingEnemy.isEnemyDead))
            {
                if (killCoroutine != null)
                {
                    StopCoroutine(killCoroutine);
                    killCoroutine = null;
                }
                killingPlayer = null;
                killingEnemy = null;
            }
            if (selectCooldown > 0)
            {
                selectCooldown -= Time.deltaTime;
            }
            if ((!isHeld && !isHeldByEnemy) || isPocketed)
            {
                return;
            }
            int currentLivingPlayers = StartOfRound.Instance.livingPlayers;
            float currentEnemyPower = TotalEnemyPower;
            if (lastLivingPlayers == currentLivingPlayers && Mathf.Approximately(lastEnemyPower, currentEnemyPower))
            {
                return;
            }
            CheckDead();
            lastLivingPlayers = currentLivingPlayers;
            lastEnemyPower = currentEnemyPower;
        }
        internal override void WildCardUse()
        {
            base.WildCardUse();
            if (!IsOwner || (!isHeld && !isHeldByEnemy) || selectCooldown > 0f)
            {
                return;
            }
            selectCooldown = 5f;
            if ((isHeld && !isPocketed) || (isHeldByEnemy && Random.Next(0, 15) == 0))
            {
                int id = AllPages.KeyOf(currentElement);
                if (!currentlyEnemy)
                {
                    KillPlayer(id);
                    return;
                }
                if (!enemyIndices.TryGetValue(id, out EnemyAI enemy))
                {
                    return;
                }
                KillEnemy(enemy);
            }
            Audio["Countdown"].PlayRandomOneshot(false);
            Transform rayStart;
            if (isHeld)
            {
                rayStart = LastPlayerHeldBy.gameplayCamera.transform;
            }
            else
            {
                rayStart = LastEnemyHeldBy.eye;
            }
            float hitDistance = addPageDistance;
            RaycastHit[] hits = Physics.SphereCastAll(new Ray(rayStart.position, rayStart.forward), 0.5f, hitDistance, pageMask, QueryTriggerInteraction.Collide);
            if (hits.Length == 0)
            {
                return;
            }
            PlayerControllerB targetPlayer = null;
            EnemyAI targetEnemy = null;
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit newHit = hits[i];
                PlayerControllerB newPlayer = newHit.transform.GetComponent<PlayerControllerB>();
                EnemyAICollisionDetect newEnemy = newHit.transform.GetComponent<EnemyAICollisionDetect>();
                if (newHit.distance > hitDistance)
                {
                    continue;
                }
                if (newPlayer == null && newEnemy == null)
                {
                    continue;
                }
                if (newPlayer != null && (isHeldByEnemy || newPlayer != LastPlayerHeldBy))
                {
                    hitDistance = newHit.distance;
                    targetPlayer = newPlayer;
                    continue;
                }
                if (newEnemy == null || (isHeldByEnemy && newEnemy == LastEnemyHeldBy) || newEnemy.mainScript.enemyType.enemyName == "Cadaver Growths")
                {
                    continue;
                }
                hitDistance = newHit.distance;
                targetEnemy = newEnemy.mainScript;
            }
            int targetID;
            if (targetPlayer != null)
            {
                targetID = (int)targetPlayer.playerClientId;
                if (playerPages.ContainsKey(targetID))
                {
                    Log.LogDebug($"{targetPlayer.playerUsername} was already in the players list!");
                    return;
                }
                NewPlayer(targetID);
                return;
            }
            Log.LogDebug("No valid player, checking enemies");
            if (targetEnemy == null)
            {
                Log.LogDebug("No valid enemy, no new page added");
                return;
            }
            targetID = targetEnemy.thisEnemyIndex;
            if (enemyPages.ContainsKey(targetID))
            {
                Log.LogDebug($"{targetEnemy.enemyType.enemyName} {targetID} was already in the players list!");
                return;
            }
            NewEnemy(targetEnemy);
        }
        private void StartOpening()
        {
            currentElement = 0;
            currentlyEnemy = false;
            SmithNoteInfo info = AllPages[index: currentElement];
            if (!info.initialized)
            {
                CheckDead();
            }
            textMeshList[0].text = info.targetName;
            rawImageList[0].texture = info.texture;
            rawImageList[0].color = info.colour;
            textMeshList[0].rectTransform.parent.gameObject.SetActive(true);
            rawImageList[0].rectTransform.parent.gameObject.SetActive(true);
            Animator.Trigger("OpenBook");
        }
        private void StartClosing()
        {
            textMeshList[1].rectTransform.parent.gameObject.SetActive(false);
            rawImageList[1].rectTransform.parent.gameObject.SetActive(false);
            isFlippable = false;
            Animator.Trigger("CloseBook");
        }
        private void StartFlipping()
        {
            isFlippable = false;
            SmithNoteInfo info = AllPages[index: currentElement];
            rawImageList[1].texture = info.texture;
            rawImageList[1].color = info.colour;
            currentElement++;
            if (currentElement >= AllPages.Count)
            {
                currentElement = 0;
            }
            currentlyEnemy = currentElement >= playerPages.Count;
            Log.LogDebug($"Page index: {currentElement}");
            textMeshList[1].text = AllPages[index: currentElement].targetName;
            textMeshList[1].rectTransform.parent.gameObject.SetActive(true);
            rawImageList[1].rectTransform.parent.gameObject.SetActive(true);
            rawImageList[0].rectTransform.parent.gameObject.SetActive(false);
        }
        public void FinishOpening()
        {
            isFlippable = true;
        }
        public void FinishClosing()
        {
            if (isHeld)
            {
                return;
            }
            textMeshList[0].rectTransform.parent.gameObject.SetActive(false);
            rawImageList[0].rectTransform.parent.gameObject.SetActive(false);
        }
        public void FinishFlipping()
        {
            textMeshList[0].text = AllPages[index: currentElement].targetName;
            textMeshList[1].rectTransform.parent.gameObject.SetActive(false);
            rawImageList[1].rectTransform.parent.gameObject.SetActive(false);
            isFlippable = true;
        }
        public void PfpFrameUpdate()
        {
            SmithNoteInfo info = AllPages[index: currentElement];
            rawImageList[0].texture = info.texture;
            rawImageList[0].color = info.colour;
            rawImageList[0].rectTransform.parent.gameObject.SetActive(true);
        }
        private void CheckDead()
        {
            int playerPageCount = playerPages.Count;
            int[] allIDs = playerPages.Keys.Concat(enemyPages.Keys).ToArray();
            for (int i = 0; i < allIDs.Length; i++)
            {
                int id = allIDs[i];
                bool doEnemy = i >= playerPageCount;
                SmithNoteInfo info;
                bool markDead;
                string baseName;
                if (doEnemy)
                {
                    EnemyAI enemy = enemyIndices[id];
                    info = enemyPages[target: id];
                    markDead = enemy.isEnemyDead || (killingEnemy == enemy && (enemy.enemyType.canBeDestroyed || enemy.enemyType.canDie));
                    baseName = WildUtils.TrueEnemyNames[enemy.enemyType.enemyName];
                }
                else
                {
                    PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[id];
                    info = playerPages[target: id];
                    markDead = player.isPlayerDead || killingPlayer == player;
                    baseName = player.playerUsername;
                }
                if (info.initialized && info.isDead == markDead)
                {
                    continue;
                }
                info.initialized = true;
                Color avatarColour = Color.white;
                info.targetName = baseName;
                if (markDead)
                {
                    info.targetName = string.Concat("<s>", info.targetName);
                    avatarColour.g *= 0.5f;
                    avatarColour.b *= 0.5f;
                }
                info.isDead = markDead;
                info.colour = avatarColour;
                for (int j = 0; j < textMeshList.Count - 1; j++)
                {
                    TextMeshProUGUI text = textMeshList[j];
                    if (!text.text.EndsWith(baseName))
                    {
                        continue;
                    }
                    text.text = info.targetName;
                }
                for (int j = 0; j < rawImageList.Count; j++)
                {
                    RawImage image = rawImageList[j];
                    if (image.texture != info.texture)
                    {
                        continue;
                    }
                    image.color = info.colour;
                }
            }
        }
        private IEnumerator KillCoroutine(PlayerControllerB localPlayer)
        {
            yield return new WaitForSeconds(4.5f);
            if (ILifeSaver.TrySave(localPlayer))
            {
                yield break;
            }
            localPlayer.KillPlayer(Vector3.up, true, CauseOfDeath.Unknown, 1);
        }
        private IEnumerator KillEnemyCoroutine(EnemyAI enemy)
        {
            yield return new WaitForSeconds(4.5f);
            enemy.KillEnemy(!enemy.enemyType.canDie);
            CheckDead();
        }
        private void SetHUD()
        {
            if (localHudAnim != null)
            {
                return;
            }
            hudTextures.SetParent(GameNetworkManager.Instance.localPlayerController.playerHudUIContainer);
            hudTextures.localPosition = Vector3.zero;
            hudTextures.localScale = Vector3.one * 0.75f;
            hudTextures.localRotation = Quaternion.identity;
            hudTextures.gameObject.SetActive(true);
            localHudAnim = hudTextures.GetComponent<Animator>();
        }
        private void NewPlayer(int id, bool networked = true)
        {
            PlayerControllerB selectingPlayer = StartOfRound.Instance.allPlayerScripts[id];
            Log.LogDebug($"Adding player {selectingPlayer.playerUsername} to players list!");
            playerPages.Add(id, new SmithNoteInfo(selectingPlayer));
            if (!networked)
            {
                return;
            }
            Audio["Select"].PlayRandomOneshot(false);
            NewPlayerRpc(id);
        }
        private void NewEnemy(EnemyAI enemy, bool networked = true)
        {
            if (enemy == null)
            {
                return;
            }
            int id = enemy.thisEnemyIndex;
            enemyIndices.Add(id, enemy);
            Log.LogDebug($"Adding enemy {enemy.enemyType.enemyName} {id} to players list!");
            enemyPages.Add(id, new SmithNoteInfo(enemy));
            if (!networked)
            {
                return;
            }
            Audio["Select"].PlayRandomOneshot(false);
            NewEnemyRpc(enemy.NetworkObject);
        }
        private void KillEnemy(EnemyAI enemy, bool networked = true)
        {
            int id = enemy.thisEnemyIndex;
            Log.LogDebug($"Attempting Kill of enemy with enemyIndex: {id}");
            killingEnemy = enemy;
            if (!killingEnemy.isEnemyDead)
            {
                killCoroutine = StartCoroutine(KillEnemyCoroutine(killingEnemy));
                Log.LogDebug($"Smith Note Killing {killingEnemy.enemyType.enemyName} {killingEnemy.thisEnemyIndex}!");
            }
            CheckDead();
            if (!networked)
            {
                return;
            }
            Audio["Write"].PlayRandomClip();
            KillEnemyRpc(enemy.NetworkObject);
        }
        private void KillPlayer(int id, bool networked = true)
        {
            killingPlayer = StartOfRound.Instance.allPlayerScripts[id];
            if (!killingPlayer.isPlayerDead)
            {
                Log.LogDebug($"Attempting Kill of player with client id: {id}");
                if (killingPlayer.IsLocal())
                {
                    Audio["Kill"].PlayRandomOneshot(false);
                    Log.LogDebug($"Smith Note Killing This Player!");
                    killingPlayer.JumpToFearLevel(1f);
                    localHudAnim?.SetTrigger("Kill");
                    killCoroutine = StartCoroutine(KillCoroutine(killingPlayer));
                }
                Log.LogDebug($"Smith Note Killing {killingPlayer.playerUsername}!");
            }
            CheckDead();
            if (!networked)
            {
                return;
            }
            Audio["Write"].PlayRandomClip();
            KillPlayerRpc(id);
        }
        [Rpc(SendTo.NotMe)]
        private void NewPlayerRpc(int id)
        {
            NewPlayer(id, false);
        }
        [Rpc(SendTo.NotMe)]
        private void NewEnemyRpc(NetworkObjectReference enemy)
        {
            if (!enemy.TryGet(out NetworkObject networkObject))
            {
                Log.LogError("Network Object of enemy could not be found while trying to add a page to Smith Note!");
                return;
            }
            NewEnemy(networkObject.GetComponent<EnemyAI>(), false);
        }
        [Rpc(SendTo.NotMe)]
        private void KillEnemyRpc(NetworkObjectReference enemy)
        {
            if (!enemy.TryGet(out NetworkObject networkObject))
            {
                Log.LogError("Network Object of enemy could not be found while trying to kill an enemy with the Smith Note!");
                return;
            }
            KillEnemy(networkObject.GetComponent<EnemyAI>(), false);
        }
        [Rpc(SendTo.NotMe)]
        private void KillPlayerRpc(int id)
        {
            KillPlayer(id, false);
        }
    }
    internal class SmithNoteInfo
    {
        internal static Texture2D playerDefault = null;
        internal static Texture2D enemyDefault = null;
        internal bool initialized = false;
        internal string targetName = string.Empty;
        internal Color colour = Color.white;
        internal Texture2D texture = null;
        internal bool isDead = false;
        internal SmithNoteInfo(PlayerControllerB player)
        {
            targetName = player.playerUsername;
            isDead = player.isPlayerDead;
            if (GameNetworkManager.Instance.disableSteam)
            {
                texture = playerDefault;
                return;
            }
            GetProfilePicture(player.playerSteamId);
        }
        internal SmithNoteInfo(EnemyAI enemy)
        {
            targetName = enemy.enemyType.enemyName;
            isDead = enemy.isEnemyDead;
            if (SmithNote.enemyPageTextures.TryGetValue(enemy.enemyType.enemyName, out texture))
            {
                return;
            }
            texture = enemyDefault;
        }
        private async void GetProfilePicture(ulong steamID)
        {
            try
            {
                texture = HUDManager.GetTextureFromImage(await SteamFriends.GetLargeAvatarAsync(steamID));
            }
            catch (Exception exception)
            {
                WildCardMod.Instance.Log.LogError(exception);
                texture = playerDefault;
            }
        }
    }
}