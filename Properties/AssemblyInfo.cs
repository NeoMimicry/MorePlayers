using System.Reflection;
using MelonLoader;

[assembly: AssemblyTitle(MorePlayers.BuildInfo.Description)]
[assembly: AssemblyDescription(MorePlayers.BuildInfo.Description)]
[assembly: AssemblyCompany(MorePlayers.BuildInfo.Company)]
[assembly: AssemblyProduct(MorePlayers.BuildInfo.Name)]
[assembly: AssemblyCopyright("Created by " + MorePlayers.BuildInfo.Author)]
[assembly: AssemblyTrademark(MorePlayers.BuildInfo.Company)]
[assembly: AssemblyVersion(MorePlayers.BuildInfo.Version)]
[assembly: AssemblyFileVersion(MorePlayers.BuildInfo.Version)]
[assembly: MelonInfo(typeof(MorePlayers.MorePlayersMod), MorePlayers.BuildInfo.Name, MorePlayers.BuildInfo.Version, MorePlayers.BuildInfo.Author, MorePlayers.BuildInfo.DownloadLink)]
[assembly: MelonColor()]

// Create and Setup a MelonGame Attribute to mark a Melon as Universal or Compatible with specific Games.
// If no MelonGame Attribute is found or any of the Values for any MelonGame Attribute on the Melon is null or empty it will be assumed the Melon is Universal.
// Values for MelonGame Attribute can be found in the Game's app.info file or printed at the top of every log directly beneath the Unity version.
[assembly: MelonGame(null, null)]