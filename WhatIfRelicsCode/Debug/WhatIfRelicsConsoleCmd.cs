using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using WhatIfRelics.WhatIfRelicsCode.Patches;

namespace WhatIfRelics.WhatIfRelicsCode.Debug;

public sealed class WhatIfRelicsConsoleCmd : AbstractConsoleCmd
{
    private static readonly string[] RootCommands = ["refresh"];

    public override string CmdName => "whatifrelics";

    public override string Args => "refresh";

    public override string Description => "WhatIfRelics debug tools. `refresh` rerolls the current 3 What If relic options.";

    public override bool IsNetworked => false;

    public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
    {
        if (args.Length <= 1)
        {
            var partial = args.Length == 0 ? string.Empty : args[0];
            return CompleteArgument(RootCommands, [], partial, CompletionType.Subcommand);
        }

        return base.GetArgumentCompletions(player, args);
    }

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (args.Length == 0 || !args[0].Equals("refresh", StringComparison.OrdinalIgnoreCase))
        {
            return new CmdResult(success: false, "Usage: whatifrelics refresh");
        }

        return StartingAncientOptionsPatch.RefreshCurrentWhatIfOptions();
    }
}
