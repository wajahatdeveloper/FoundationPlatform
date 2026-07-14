using System;

namespace AetherNexus.FoundationPlatform.Behaviours
{
	/// <summary>
	///  Signals when deterministic scene initialization has finished seeding gameplay RNG.
	///  Wired by <c>SceneInitializationCoordinator</c> in GameEngineCore; consumed by scene spawners in FoundationPlatform.
	/// </summary>
	public static class SceneSpawnReadyGate
	{
		public static bool UsesDeterministicStartup { get; private set; }
		public static bool IsReady { get; private set; }

		public static event Action OnReady;

		public static void BeginDeterministicSceneLoad()
		{
			UsesDeterministicStartup = true;
			IsReady = false;
			OnReady = null;
		}

		public static void MarkReady()
		{
			if (!UsesDeterministicStartup || IsReady) return;

			IsReady = true;
			OnReady?.Invoke();
		}
	}
}
