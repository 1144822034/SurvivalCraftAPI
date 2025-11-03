#if !IOS
using Engine;
using GameEntitySystem;
using Jint;
using Jint.Native.Function;

namespace Game {
    public class JsModLoader : ModLoader {
        public override void OnMinerDig(ComponentMiner miner, TerrainRaycastResult raycastResult, ref float DigProgress, out bool Digged) {
            bool Digged1 = false;
            if (JsInterface.handlersDictionary.TryGetValue("OnMinerDig", out List<Function> functions)) {
                float DigProgress1 = DigProgress;
                foreach (Function function in functions) {
                    Digged1 |= JsInterface.Invoke(function, miner, raycastResult, DigProgress1).AsBoolean();
                }
            }
            Digged = Digged1;
        }

        public override void OnMinerPlace(ComponentMiner miner,
            TerrainRaycastResult raycastResult,
            int x,
            int y,
            int z,
            int value,
            out bool Placed) {
            bool Placed1 = false;
            if (JsInterface.handlersDictionary.TryGetValue("OnMinerPlace", out List<Function> functions)) {
                foreach (Function function in functions) {
                    Placed1 |= JsInterface.Invoke(
                            function,
                            miner,
                            raycastResult,
                            x,
                            y,
                            z,
                            value
                        )
                        .AsBoolean();
                }
            }
            Placed = Placed1;
        }

        public override bool OnPlayerSpawned(PlayerData.SpawnMode spawnMode, ComponentPlayer componentPlayer, Vector3 position) {
            if (JsInterface.handlersDictionary.TryGetValue("OnPlayerSpawned", out List<Function> functions)) {
                foreach (Function function in functions) {
                    JsInterface.Invoke(function, spawnMode, componentPlayer, position);
                }
            }
            return false;
        }

        public override void OnPlayerDead(PlayerData playerData) {
            if (JsInterface.handlersDictionary.TryGetValue("OnPlayerDead", out List<Function> functions)) {
                foreach (Function function in functions) {
                    JsInterface.Invoke(function, playerData);
                }
            }
        }

        public override void ProcessAttackment(Attackment attackment) {
            if (JsInterface.handlersDictionary.TryGetValue("ProcessAttackment", out List<Function> functions)) {
                foreach (Function function in functions) {
                    JsInterface.Invoke(function, attackment);
                }
            }
        }

        public override void CalculateCreatureInjuryAmount(Injury injury) {
            if (JsInterface.handlersDictionary.TryGetValue("CalculateCreatureInjuryAmount", out List<Function> functions)) {
                foreach (Function function in functions) {
                    JsInterface.Invoke(function, injury);
                }
            }
        }

        public override void OnProjectLoaded(Project project) {
            if (JsInterface.handlersDictionary.TryGetValue("OnProjectLoaded", out List<Function> functions)) {
                foreach (Function function in functions) {
                    JsInterface.Invoke(function, project);
                }
            }
        }

        public override void OnProjectDisposed() {
            if (JsInterface.handlersDictionary.TryGetValue("OnProjectDisposed", out List<Function> functions)) {
                foreach (Function function in functions) {
                    JsInterface.Invoke(function);
                }
            }
        }
    }
}

#else


using Engine;
using GameEntitySystem;
using JavaScriptCore;
using static JavaScriptCore.JSValue;

namespace Game {
    public class JsModLoader : ModLoader {
        public JSContext JSContext;
        public override void OnMinerDig(ComponentMiner miner, TerrainRaycastResult raycastResult, ref float DigProgress, out bool Digged) {
            bool Digged1 = false;
            if (JsInterface.handlersDictionary.TryGetValue("OnMinerDig", out List<JSValue> functions)) {
                float DigProgress1 = DigProgress;
                foreach (JSValue function in functions) {
                }
            }
            Digged = Digged1;
        }

        public override void OnMinerPlace(ComponentMiner miner,
            TerrainRaycastResult raycastResult,
            int x,
            int y,
            int z,
            int value,
            out bool Placed) {
            bool Placed1 = false;
            if (JsInterface.handlersDictionary.TryGetValue("OnMinerPlace", out List<JSValue> functions)) {

            }
            Placed = Placed1;
        }

        public override bool OnPlayerSpawned(PlayerData.SpawnMode spawnMode, ComponentPlayer componentPlayer, Vector3 position) {
            if (JsInterface.handlersDictionary.TryGetValue("OnPlayerSpawned", out List<JSValue> functions)) {
            }
            return false;
        }

        public override void OnPlayerDead(PlayerData playerData) {
            if (JsInterface.handlersDictionary.TryGetValue("OnPlayerDead", out List<JSValue> functions)) {
                foreach (JSValue function in functions) {
                }
            }
        }

        public override void ProcessAttackment(Attackment attackment) {
            if (JsInterface.handlersDictionary.TryGetValue("ProcessAttackment", out List<JSValue> functions)) {
                foreach (JSValue function in functions) {
                }
            }
        }

        public override void CalculateCreatureInjuryAmount(Injury injury) {
            if (JsInterface.handlersDictionary.TryGetValue("CalculateCreatureInjuryAmount", out List<JSValue> functions)) {
                foreach (JSValue function in functions) {
                }
            }
        }

        public override void OnProjectLoaded(Project project) {
            if (JsInterface.handlersDictionary.TryGetValue("OnProjectLoaded", out List<JSValue> functions)) {
                foreach (JSValue function in functions) {
                }
            }
        }

        public override void OnProjectDisposed() {
            if (JsInterface.handlersDictionary.TryGetValue("OnProjectDisposed", out List<JSValue> functions)) {
                foreach (JSValue function in functions) {
                }
            }
        }
    }
}
#endif