using System;
using System.Collections.Generic;

namespace Fusion.Collywobbles.Futsal
{
  public class CommandLineUtils {

    /// <summary>
    /// Signal if the executable was started in Headless mode by using the "-batchmode -nographics" command-line arguments
    /// <see cref="https://docs.unity3d.com/Manual/PlayerCommandLineArguments.html"/>
    /// </summary>
    /// <returns>True if in "Headless Mode", false otherwise</returns>
    public static bool IsHeadlessMode() {
      return Environment.CommandLine.Contains("-batchmode") && Environment.CommandLine.Contains("-nographics");
    }

    /// <summary>
    /// Get a list tuple of arguments starting with a specific prefix.
    /// </summary>
    /// <param name="prefix">Prefix tested on each argument</param>
    /// <returns>List of tuples with argument name and argument value</returns>
    public static List<(string, string)> GetArgumentList(string prefix) {

      var output = new List<(string, string)>();

      var args = Environment.GetCommandLineArgs();

      for (int i = 0; i < args.Length; i++) {

        if (args[i].Trim().StartsWith(prefix) && args.Length > i + 1) {
          var key = args[i].Trim().Replace(prefix, "");
          var value = args[i + 1];

          output.Add((key, value));
        }
      }

      return output;
    }

    /// <summary>
    /// Get the value of a specific command-line argument passed when starting the executable
    /// </summary>
    /// <example>
    /// Starting the binary with: "./my-game.exe -map street -type hide-and-seek"
    /// and calling `var mapValue = HeadlessUtils.GetArg("-map", "-m")` will return the string "street"
    /// </example>
    /// <param name="keys">List of possible keys for the argument</param>
    /// <returns>The string value of the argument if the at least 1 key was found, null otherwise</returns>
    public static bool TryGetArg(string argName, out string argValue) {

      var args = Environment.GetCommandLineArgs();
      argValue = null;

      for (int i = 0; i < args.Length; i++) {
        if (args[i].Equals(argName) && args.Length > i + 1) {

          argValue = args[i + 1];
          return true;
        }
      }

      return false;
    }
  }
}