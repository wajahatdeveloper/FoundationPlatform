namespace AetherNexus.FoundationPlatform
{
    /// <summary>
    ///     How long a persistent (DontDestroyOnLoad) object is meant to live. Declared at registration so
    ///     the lifetime is readable from the code that owns it and checkable by editor validation, instead
    ///     of being implied by a bare <c>DontDestroyOnLoad</c> call.
    /// </summary>
    public enum PersistenceScope
    {
        /// <summary>
        ///     Lives for the whole process: audio, tween/coroutine runners, dispatchers, engine singletons.
        ///     A session boundary never touches it — this is the scope that carries music across a scene load.
        /// </summary>
        Application,

        /// <summary>
        ///     Lives for a play session: bootstrap, simulation loop, session root, player controllers.
        ///     Its owner is expected to reset or tear down its state when the session ends.
        /// </summary>
        Session
    }
}
