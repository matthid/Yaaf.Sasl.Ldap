// ----------------------------------------------------------------------------
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.
// ----------------------------------------------------------------------------

(*
    This file handles the complete build process of RazorEngine

    The first step is handled in build.sh and build.cmd by bootstrapping a NuGet.exe and 
    executing NuGet to resolve all build dependencies (dependencies required for the build to work, for example FAKE)

    The secound step is executing this file which resolves all dependencies, builds the solution and executes all unit tests
*)


// Supended until FAKE supports custom mono parameters
#I @".nuget/Build/FAKE/tools/" // FAKE
#r @"FakeLib.dll"  //FAKE

open System.Collections.Generic
open System.IO

open Fake
open Fake.Git
open Fake.FSharpFormatting
open AssemblyInfoFile


// properties (main)
let projectName = "Yaaf.Sasl"
let copyrightNotice = "Yaaf.Sasl Copyright © Matthias Dittrich 2011-2015"
let projectSummary = "Yaaf.Sasl is a simple .net library for SASL."
let projectDescription = "Yaaf.Sasl is a SASL .net library."
let authors = ["Matthias Dittrich"]
let page_author = "Matthias Dittrich"
let mail = "matthi.d@gmail.com"
let version = "1.0.0.0"
let version_nuget = "1.0.0"
let commitHash = Information.getCurrentSHA1(".")

// properties ldap
let projectName_ldap = "Yaaf.Sasl.Ldap"
let projectSummary_ldap = "Yaaf.Sasl.Ldap is a server LDAP backend for Yaaf.Sasl."
let projectDescription_ldap = "Yaaf.Sasl.Ldap is a server LDAP backend for Yaaf.Sasl."
let version_ldap = "1.0.0.0"
let version_nuget_ldap = "1.0.0"

let github_user = "matthid"
let github_project = "Yaaf.Sasl"
let nuget_url = "https://www.nuget.org/packages/Yaaf.Sasl/"

let tags = "sasl C# F# dotnet .net ldap"

let generated_file_list =
  [ "Yaaf.Sasl.dll"
    "Yaaf.Sasl.xml"
    "Yaaf.Sasl.Ldap.dll"
    "Yaaf.Sasl.Ldap.xml" ]

type BuildParams =
    {
        SimpleBuildName : string
        CustomBuildName : string
    }

let net40Params = { SimpleBuildName = "net40"; CustomBuildName = "net40" }
let net45Params = { SimpleBuildName = "net45"; CustomBuildName = "net45" }
let profile111Params = { SimpleBuildName = "profile111"; CustomBuildName = "portable-net45+netcore45+wpa81+MonoAndroid1+MonoTouch1" }

let allParams = [ net40Params; net45Params; profile111Params ]

let use_nuget = true

let buildDir = "./build/"
let releaseDir = "./release/"
let outLibDir = "./release/lib/"
let outDocDir = "./release/documentation/"
let docTemplatesDir = "./doc/templates/"
let testDir  = "./test/"
let nugetDir  = "./.nuget/"
let packageDir  = "./.nuget/packages"

let buildMode = "Release" // if isMono then "Release" else "Debug"

// Where to look for *.cshtml templates (in this order)
let layoutRoots =
    [ docTemplatesDir; 
      docTemplatesDir @@ "reference" ]

if isMono then
    monoArguments <- "--runtime=v4.0 --debug"
    //monoArguments <- "--runtime=v4.0"

let github_url = sprintf "https://github.com/%s/%s" github_user github_project

// Read release notes document
let release = ReleaseNotesHelper.parseReleaseNotes (File.ReadLines "doc/ReleaseNotes.md")

let nugetPackages =
  [ "Yaaf.Sasl.nuspec", (fun p ->
      { p with
          Authors = authors
          Project = projectName
          Summary = projectSummary
          Description = projectDescription
          Version = version_nuget
          ReleaseNotes = toLines release.Notes
          Tags = tags
          Dependencies = [ ] })
    "Yaaf.Sasl.Ldap.nuspec", (fun p ->
      { p with
          Authors = authors
          Project = projectName_ldap
          Summary = projectSummary_ldap
          Description = projectDescription_ldap
          Version = version_nuget_ldap
          ReleaseNotes = toLines release.Notes
          Tags = tags
          Dependencies = [ projectName, version_nuget ] }) ]
    
let findProjectFiles (buildParams:BuildParams) =
    !! (sprintf "src/source/**/*.%s.fsproj" buildParams.CustomBuildName)
    ++ (sprintf "src/source/**/*.%s.csproj" buildParams.CustomBuildName)

let findTestFiles (buildParams:BuildParams) =
    !! (sprintf "src/test/**/Test.*.%s.fsproj" buildParams.CustomBuildName)
    ++ (sprintf "src/test/**/Test.*.%s.csproj" buildParams.CustomBuildName)
