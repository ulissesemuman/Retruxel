using System;

namespace Retruxel.Core.Services;

/// <summary>
/// Thrown when a build-time constraint is violated (e.g. VRAM overflow, SAT overflow).
/// Message is user-facing and shown in the Build Console.
/// </summary>
public class BuildException(string message) : Exception(message);
