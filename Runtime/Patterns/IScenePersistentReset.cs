namespace AetherNexus.FoundationPlatform
{
    /// <summary>
    ///     Implemented by components under a registered persistent object that must re-arm at a scene
    ///     boundary. The broadcast comes from the engine's scene-enter/exit path via
    ///     <see cref="PersistentObjects" />, so a persistent object never subscribes to
    ///     <c>SceneManager.sceneLoaded</c> on its own and never runs its reset in the wrong order.
    /// </summary>
    public interface IScenePersistentReset
    {
        /// <summary>Called after the outgoing scene has torn down its world, before the next scene loads.</summary>
        void OnScenePersistentExit();

        /// <summary>Called on the incoming scene, before its world is built.</summary>
        void OnScenePersistentEnter();
    }
}
