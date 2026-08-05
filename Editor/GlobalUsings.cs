// Resolves the DebugX namespace vs static DebugX class name clash for code under
// AetherNexus.FoundationPlatform.* (sibling of the DebugX namespace).
global using DebugX = AetherNexus.FoundationPlatform.DebugX.DebugX;
