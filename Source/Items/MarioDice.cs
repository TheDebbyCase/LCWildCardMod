using GameNetcodeStuff;
using LCWildCardMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
namespace LCWildCardMod.Items
{
    public class MarioDice : WildCardThrowable
    {
        private static GameObject[] maskPrefabs = null;
        [Space(3f)]
        [Header("MarioDice")]
        [Space(3f)]
        [SerializeField]
        private float diceGracePeriod = 1.5f;
        [SerializeField]
        private float explodeKillRadius = 2f;
        [SerializeField]
        private float explodeDamageRadius = 5f;
        [SerializeField]
        private int explodePlayerDamage = 35;
        [SerializeField]
        private string[] maskNames = new string[] { "TragedyMask", "ComedyMask" };
        [SerializeField]
        private Vector2Int bugsMinMax = new Vector2Int(3, 6);
        [SerializeField]
        private Vector2Int wormsMinMax = new Vector2Int(3, 5);
        [SerializeField]
        private AudioClip teleportAudio = null;
        [SerializeField]
        private int coroutineLoopMax = 120;
        private int currentSide = 1;
        private int startingValue = -1;
        private bool animFinished = false;
        private bool rollable = false;
        private bool spawnedBugs = false;
        private bool spawnedWorms = false;
        private HoarderBugAI[] currentBugs = null;
        private SandWormAI[] currentWorms = null;
        private Coroutine effectCoroutine = null;
        public override void Start()
        {
            base.Start();
            if (startingValue < 0)
            {
                startingValue = ScrapValue;
            }
            if (maskPrefabs != null)
            {
                return;
            }
            maskPrefabs = new GameObject[maskNames.Length];
            for (int i = 0; i < maskNames.Length; i++)
            {
                string mask = maskNames[i];
                if (maskPrefabs[i] != null)
                {
                    continue;
                }
                maskPrefabs[i] = StartOfRound.Instance.allItemsList.itemsList.Find(x => x.name == mask)?.spawnPrefab;
            }
        }
        internal override void Throw(Vector3 newPosition, bool byEnemy, bool networked = true)
        {
            if (IsOwner)
            {
                SetSide(Random.Next(1, 7));
                Audio["Roll"].PlayRandomClip();
            }
            rollable = true;
            base.Throw(newPosition, byEnemy, networked);
        }
        private void SetSide(int side, bool networked = true)
        {
            animFinished = false;
            currentSide = side;
            Animator.SetInt("Side", currentSide);
            if (!networked)
            {
                return;
            }
            SetSideRpc(side);
        }
        public override void OnHitGround()
        {
            base.OnHitGround();
            if (!IsServer || !hasBeenHeld || !rollable)
            {
                return;
            }
            if (effectCoroutine != null)
            {
                StopCoroutine(effectCoroutine);
                effectCoroutine = null;
            }
            effectCoroutine = StartCoroutine(DiceEffectCoroutine(currentSide));
        }
        public void AnimationFinished()
        {
            animFinished = true;
        }
        private IEnumerator DiceEffectCoroutine(int side)
        {
            Log.LogDebug($"Running dice roll {side}");
            yield return new WaitUntil(() => animFinished);
            EffectToggle(false);
            SelectAudioClips audio = Audio["Negative"];
            if (side >= 4)
            {
                audio = Audio["Positive"];
            }
            audio.PlayRandomOneshot();
            yield return new WaitForSeconds(diceGracePeriod);
            if (isHeld || isHeldByEnemy || !StartOfRound.Instance.currentLevel.planetHasTime || StartOfRound.Instance.inShipPhase)
            {
                EffectToggle(true);
                yield break;
            }
            switch (side)
            {
                default:
                    {
                        if (lastThrownByEnemy)
                        {
                            Swarm();
                            break;
                        }
                        ForceMask();
                        break;
                        
                    }
                case 2:
                    {
                        if (ScrapValue <= 0)
                        {
                            Explode();
                            break;
                        }
                        ValueZero();
                        break;
                    }
                case 3:
                    {
                        Swarm();
                        break;
                    }
                case 4:
                    {
                        if (StartOfRound.Instance.connectedPlayersAmount < 1)
                        {
                            TeleportSelf();
                            break;
                        }
                        List<PlayerControllerB> players = new List<PlayerControllerB>();
                        for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
                        {
                            PlayerControllerB playerToAdd = StartOfRound.Instance.allPlayerScripts[i];
                            if (!playerToAdd.isPlayerControlled || playerToAdd.isPlayerDead || (!lastThrownByEnemy && playerToAdd == LastPlayerHeldBy))
                            {
                                continue;
                            }
                            players.Add(playerToAdd);
                        }
                        if (players.Count == 0)
                        {
                            TeleportSelf();
                            break;
                        }
                        PlayerSwap(players);
                        break;
                    }
                case 5:
                    {
                        TeleportSelf();
                        break;
                    }
                case 6:
                    {
                        ValueIncrease();
                        break;
                    }
            }
        }
        private void EffectToggle(bool enable, bool networked = true)
        {
            EnablePhysics(enable);
            grabbable = enable;
            grabbableToEnemies = enable;
            if (!enable)
            {
                rollable = false;
                Audio["Roll"].Stop(false);
                Particles["Effects"].SetMaterialsTexture(0, Mathf.Clamp(Animator.GetInt("Side") - 1, 0, 5));
                Particles["Effects"].PlayAll(networked: false);
            }
            if (!networked)
            {
                return;
            }
            ToggleEffectRpc(enable);
        }
        private void ValueZero()
        {
            ScrapValue = 0;
            EffectToggle(true);
        }
        private void Explode()
        {
            ExplodeRpc();
        }
        private void ForceMask()
        {
            if (maskPrefabs == null || maskPrefabs.Length == 0)
            {
                EffectToggle(true);
                return;
            }
            GameObject maskTarget = maskPrefabs[Random.Next(0, maskPrefabs.Length)];
            if (maskTarget == null)
            {
                EffectToggle(true);
                return;
            }
            HauntedMaskItem mask = Instantiate(maskTarget).GetComponent<HauntedMaskItem>();
            NetworkObject maskObject = mask.NetworkObject;
            maskObject.Spawn();
            MaskRpc(maskObject, LastPlayerHeldBy.GetRPCTarget());
        }
        private IEnumerator MaskCoroutine(NetworkObjectReference maskRef)
        {
            yield return new WaitForEndOfFrame();
            NetworkObject maskObject = null;
            while (!maskRef.TryGet(out maskObject))
            {
                yield return new WaitForEndOfFrame();
            }
            PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
            HauntedMaskItem mask = maskObject.GetComponent<HauntedMaskItem>();
            player.DropAllHeldItemsAndSync(player.transform.position, player.localItemHolder.position, player.localItemHolder.eulerAngles, player.playerEye.position, player.playerEye.eulerAngles);
            yield return new WaitUntil(() => player.ItemSlots[player.currentItemSlot] == null);
            player.GrabObjectServerRpc(maskRef);
            mask.parentObject = player.localItemHolder;
            yield return new WaitUntil(() => mask.isHeld && !mask.isPocketed);
            mask.ItemActivate(true); 
            mask.BeginAttachment();
            EffectToggle(true);
        }
        private void Swarm()
        {
            if (isInFactory)
            {
                if (spawnedBugs)
                {
                    Explode();
                    return;
                }
                BugSwarm();
                return;
            }
            if (spawnedWorms)
            {
                Explode();
                return;
            }
            WormSwarm();
        }
        private void BugSwarm()
        {
            List<EnemyVent> vents = RoundManager.Instance.allEnemyVents.ToList();
            vents.Sort((x, y) => Vector3.Distance(x.floorNode.position, transform.position).CompareTo(Vector3.Distance(y.floorNode.position, transform.position)));
            int bugCount = Random.Next(bugsMinMax.x, bugsMinMax.y + 1);
            NetworkObjectReference[] refs = new NetworkObjectReference[bugCount];
            for (int i = 0; i < bugCount; i++)
            {
                int index = i;
                if (i >= vents.Count)
                {
                    index -= vents.Count;
                }
                Transform node = vents[index].floorNode;
                node ??= transform;
                refs[i] = RoundManager.Instance.SpawnEnemyGameObject(node.position, node.eulerAngles.y, -1, WildUtils.AllEnemies["Hoarding bug"]);
            }
            spawnedBugs = true;
            StartCoroutine(BugModeCoroutine(refs));
        }
        private IEnumerator BugModeCoroutine(NetworkObjectReference[] refs)
        {
            yield return new WaitForEndOfFrame();
            currentBugs = new HoarderBugAI[refs.Length];
            int loopTimes = 0;
            int bugsLoaded = 0;
            while (bugsLoaded < refs.Length && loopTimes <= coroutineLoopMax)
            {
                for (int i = 0; i < refs.Length; i++)
                {
                    if (currentBugs[i] != null || !refs[i].TryGet(out NetworkObject newBug))
                    {
                        continue;
                    }
                    currentBugs[i] = newBug.GetComponent<HoarderBugAI>();
                    bugsLoaded++;
                }
                yield return new WaitForEndOfFrame();
                loopTimes++;
            }
            EffectToggle(true);
            int bugsAlive = currentBugs.Length;
            bool[] bugNestChange = new bool[currentBugs.Length];
            while (bugsAlive > 0)
            {
                bugsAlive = currentBugs.Length;
                for (int i = 0; i < currentBugs.Length; i++)
                {
                    HoarderBugAI bug = currentBugs[i];
                    if (bug == null || bug.isEnemyDead)
                    {
                        bugsAlive--;
                        continue;
                    }
                    bug.removedPowerLevel = true;
                    if (!isHeld && !isHeldByEnemy && !bug.SetDestinationToPosition(transform.position, checkForPath: true))
                    {
                        bug.KillEnemy();
                        bug.NetworkObject.Despawn();
                        bugsAlive--;
                        continue;
                    }
                    if (!bugNestChange[i])
                    {
                        bugNestChange[i] = true;
                        bug.choseNestPosition = true;
                        bug.nestPosition = bug.ChooseClosestNodeToPosition(transform.position).position;
                        bug.SyncNestPositionClientRpc(bug.nestPosition);
                    }
                    if (bug.isAngry || bug.heldItem?.itemGrabbableObject == this || !HoarderBugAI.grabbableObjectsInMap.Contains(gameObject) || Vector3.Distance(transform.position, bug.nestPosition) < 0.75f)
                    {
                        continue;
                    }
                    bug.targetItem = this;
                }
                yield return null;
            }
        }
        private void WormSwarm()
        {
            List<GameObject> nodes = RoundManager.Instance.outsideAINodes.ToList();
            nodes.Sort((x, y) => Vector3.Distance(x.transform.position, LastPlayerHeldBy.transform.position).CompareTo(Vector3.Distance(y.transform.position, LastPlayerHeldBy.transform.position)));
            int wormCount = Random.Next(wormsMinMax.x, wormsMinMax.y + 1);
            NetworkObjectReference[] refs = new NetworkObjectReference[wormCount];
            for (int i = 0; i < wormCount; i++)
            {
                int index = i;
                if (i >= nodes.Count)
                {
                    index -= nodes.Count;
                }
                Transform node = nodes[index].transform;
                refs[i] = RoundManager.Instance.SpawnEnemyGameObject(RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(node.position, 50f, default, Random) + Vector3.up, 0f, -1, WildUtils.AllEnemies["Earth Leviathan"]);
            }
            spawnedWorms = true;
            StartCoroutine(WormModeCoroutine(refs));
        }
        private IEnumerator WormModeCoroutine(NetworkObjectReference[] refs)
        {
            yield return new WaitForEndOfFrame();
            currentWorms = new SandWormAI[refs.Length];
            int loopTimes = 0;
            int wormsLoaded = 0;
            while (wormsLoaded < refs.Length && loopTimes <= coroutineLoopMax)
            {
                for (int i = 0; i < refs.Length; i++)
                {
                    if (currentWorms[i] != null || !refs[i].TryGet(out NetworkObject newWorm))
                    {
                        continue;
                    }
                    currentWorms[i] = newWorm.GetComponent<SandWormAI>();
                    wormsLoaded++;
                }
                yield return new WaitForEndOfFrame();
                loopTimes++;
            }
            EffectToggle(true);
            yield return new WaitForSeconds(Random.Next(2, 6));
            for (int i = 0; i < currentWorms.Length; i++)
            {
                SandWormAI worm = currentWorms[i];
                if (worm == null || worm.isEnemyDead)
                {
                    continue;
                }
                worm.StartEmergeAnimation();
            }
        }
        private void PlayerSwap(List<PlayerControllerB> players)
        {
            PlayerControllerB playerToSwap = players[Random.Next(0, players.Count)];
            Vector3 playerPos = new Vector3(LastPlayerHeldBy.transform.position.x, RoundManager.Instance.GetNavMeshPosition(LastPlayerHeldBy.transform.position, RoundManager.Instance.navHit).y, LastPlayerHeldBy.transform.position.z);
            Vector3 playerToSwapPos = new Vector3(playerToSwap.transform.position.x, RoundManager.Instance.GetNavMeshPosition(playerToSwap.transform.position, RoundManager.Instance.navHit).y, playerToSwap.transform.position.z);
            TeleportMultipleRpc(new int[] { (int)LastPlayerHeldBy.playerClientId, (int)playerToSwap.playerClientId }, new Vector3[] { playerToSwapPos, playerPos }, new bool[] { playerToSwap.isInHangarShipRoom, LastPlayerHeldBy.isInHangarShipRoom }, new bool[] { playerToSwap.isInsideFactory, LastPlayerHeldBy.isInsideFactory });
        }
        private void TeleportPlayer(int id, Vector3 position, bool inShip, bool inside)
        {
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[id];
            player.averageVelocity = 0f;
            player.velocityLastFrame = Vector3.zero;
            player.TeleportPlayer(position, true);
            player.beamOutParticle.Play();
            player.isInElevator = inShip;
            player.isInHangarShipRoom = inShip;
            player.isInsideFactory = inside;
            for (int i = 0; i < player.ItemSlots.Length; i++)
            {
                GrabbableObject item = player.ItemSlots[i];
                if (item == null)
                {
                    continue;
                }
                item.isInElevator = player.isInElevator;
                item.isInShipRoom = player.isInHangarShipRoom;
                item.isInFactory = player.isInsideFactory;
            }
            player.movementAudio.PlayOneShot(teleportAudio);
            if (!player.IsLocal())
            {
                return;
            }
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
            if (inside)
            {
                TimeOfDay.Instance.DisableAllWeather();
                return;
            }
            LevelWeatherType weather = StartOfRound.Instance.currentLevel.currentWeather;
            if (weather == LevelWeatherType.None)
            {
                return;
            }
            TimeOfDay.Instance.effects[(int)weather].effectEnabled = true;
        }
        private void TeleportSelf()
        {
            if (lastThrownByEnemy)
            {
                Swarm();
                return;
            }
            int id = (int)LastPlayerHeldBy.playerClientId;
            if (LastPlayerHeldBy.isInsideFactory)
            {
                TeleportSingleRpc(id, StartOfRound.Instance.middleOfShipNode.position, true, false);
                return;
            }
            int navMask = 1537;
            int layerMask = 1375734017;
            int nodeCount = Mathf.Min(12, RoundManager.Instance.insideAINodes.Length);
            Vector3 vector = Vector3.zero;
            for (int i = 0; i < nodeCount; i++)
            {
                vector = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(RoundManager.Instance.insideAINodes[Random.Next(0, RoundManager.Instance.insideAINodes.Length)].transform.position, 10f, default, Random, navMask);
                if (!RoundManager.Instance.GotNavMeshPositionResult || !NavMesh.FindClosestEdge(vector, out NavMeshHit hit, navMask))
                {
                    continue;
                }
                Ray ray = new Ray(hit.position + Vector3.up * 0.5f, default);
                if (hit.position == vector)
                {
                    ray.direction = hit.position - new Vector3(RoundManager.Instance.randomPositionInBoxRadius.x, hit.position.y, RoundManager.Instance.randomPositionInBoxRadius.z);
                }
                else
                {
                    ray.direction = vector - hit.position;
                }
                if (Physics.Raycast(ray, out RaycastHit hitInfo, 5f, layerMask, QueryTriggerInteraction.Ignore))
                {
                    if (!(hitInfo.distance < 0.35f))
                    {
                        vector = RoundManager.Instance.GetNavMeshPosition(ray.GetPoint(hitInfo.distance / 2f), default, 2f, navMask);
                        break;
                    }
                    ray.origin += Vector3.Normalize(ray.direction * 1000f) * 0.4f;
                    if (!Physics.Raycast(ray, out hitInfo, 5f, layerMask, QueryTriggerInteraction.Ignore))
                    {
                        vector = RoundManager.Instance.GetNavMeshPosition(ray.GetPoint(2.5f), default, 2f, navMask);
                        break;
                    }
                    if (hitInfo.distance > 0.35f)
                    {
                        vector = RoundManager.Instance.GetNavMeshPosition(ray.GetPoint(hitInfo.distance / 2f), default, 2f, navMask);
                        break;
                    }
                }
                vector = RoundManager.Instance.GetNavMeshPosition(ray.GetPoint(2.5f), default, 2f, navMask);
            }
            if (vector == Vector3.zero)
            {
                return;
            }
            TeleportSingleRpc(id, vector, false, true);
        }
        private void ValueIncrease()
        {
            ScrapValue += startingValue;
            EffectToggle(true);
        }
        private IEnumerator DespawnCoroutine()
        {
            SelectParticles particles = Particles["Effects"];
            yield return new WaitUntil(() => particles == null || !particles.AnyAlive());
            yield return null;
            NetworkObject.Despawn();
        }
        public override int GetItemDataToSave()
        {
            string intBuild = $"{currentSide}0{startingValue}";
            return int.Parse(intBuild);
        }
        public override void LoadItemSaveData(int saveData)
        {
            base.LoadItemSaveData(saveData);
            ReadOnlySpan<char> stringBuild = saveData.ToString();
            SetSide(int.Parse(stringBuild[..1]), false);
            startingValue = int.Parse(stringBuild[2..]);
        }
        [Rpc(SendTo.NotMe)]
        private void SetSideRpc(int side)
        {
            SetSide(side, false);
        }
        [Rpc(SendTo.NotMe)]
        private void ToggleEffectRpc(bool enable)
        {
            EffectToggle(enable, false);
        }
        [Rpc(SendTo.Everyone)]
        private void ExplodeRpc()
        {
            Landmine.SpawnExplosion(transform.position, true, explodeKillRadius, explodeDamageRadius, explodePlayerDamage, 0.5f);
            if (isHeld && !isPocketed)
            {
                int playerOriginalSlot = LastPlayerHeldBy.currentItemSlot;
                if (LastPlayerHeldBy.currentlyHeldObjectServer != this)
                {
                    LastPlayerHeldBy.SwitchToItemSlot(Array.FindIndex(LastPlayerHeldBy.ItemSlots, x => x == this));
                }
                LastPlayerHeldBy.DiscardHeldObject();
                if (playerOriginalSlot != LastPlayerHeldBy.currentItemSlot)
                {
                    LastPlayerHeldBy.SwitchToItemSlot(playerOriginalSlot);
                }
            }
            EnableItemMeshes(false);
            EffectToggle(false, false);
            if (!IsServer)
            {
                return;
            }
            StartCoroutine(DespawnCoroutine());
        }
        [Rpc(SendTo.SpecifiedInParams)]
        private void MaskRpc(NetworkObjectReference mask, RpcParams rpcParams = default)
        {
            StartCoroutine(MaskCoroutine(mask));
        }
        [Rpc(SendTo.Everyone)]
        private void TeleportMultipleRpc(int[] ids, Vector3[] positions, bool[] inShips, bool[] insides)
        {
            for (int i = 0; i < ids.Length; i++)
            {
                TeleportPlayer(ids[i], positions[i], inShips[i], insides[i]);
            }
            EffectToggle(true, false);
        }
        [Rpc(SendTo.Everyone)]
        private void TeleportSingleRpc(int id, Vector3 position, bool inShip, bool inside)
        {
            TeleportPlayer(id, position, inShip, inside);
            EffectToggle(true, false);
        }
    }
}