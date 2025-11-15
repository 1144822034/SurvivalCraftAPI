dotnet pack Engine --configuration Release -p:TargetFrameworks=net10.0 -o ./nupkgs
dotnet pack EntitySystem --configuration Release -p:TargetFrameworks=net10.0 -o ./nupkgs
dotnet pack Survivalcraft.Windows --configuration Release -p:TargetFrameworks=net10.0 -o ./nupkgs