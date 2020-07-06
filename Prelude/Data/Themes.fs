namespace Prelude.Data

open System
open System.IO
open System.IO.Compression
open System.Drawing
open Prelude.Json
open Prelude.Common

module Themes =
    
    type StorageType = Zip of ZipArchive | Folder of string

    type ThemeConfig = {
        JudgementColors: Color array
        JudgementNames: string array
        Font: string
        TextColor: Color
        SelectChart: Color
        DefaultAccentColor: Color
        OverrideAccentColor: bool
        PlayfieldColor: Color
        CursorSize: float32
    } with
        static member Default: ThemeConfig = {
            JudgementColors = [|Color.FromArgb(127, 127, 255); Color.FromArgb(0, 255, 255); Color.FromArgb(255, 255, 0);
                Color.FromArgb(0, 255, 100); Color.FromArgb(0, 0, 255); Color.Fuchsia; Color.FromArgb(255, 0, 0); Color.FromArgb(255, 255, 0); Color.Fuchsia|];
            JudgementNames = [|"Ridiculous"; "Marvellous"; "Perfect"; "Good"; "Bad"; "Boo"; "Miss"; "OK"; "Not Good"|];
            Font = "Akrobat Black";
            TextColor = Color.White;
            SelectChart = Color.FromArgb(0, 180, 110);
            DefaultAccentColor = Color.FromArgb(0, 255, 160);
            OverrideAccentColor = false;
            PlayfieldColor = Color.FromArgb(120, 0, 0, 0);
            CursorSize = 50.0f
        }

    type NoteSkinConfig = {
        UseRotation: bool
        Name: string
        FlipHoldTail: bool
        UseHoldTailTexture: bool
        ColumnWidth: float32
        HoldNoteTrim: float32
        PlayfieldAlignment: float32 * float32
    } with
        static member Default = {
            UseRotation = false
            Name = "?"
            FlipHoldTail = true
            UseHoldTailTexture = true
            ColumnWidth = 150.0f
            HoldNoteTrim = 0.0f
            PlayfieldAlignment =  0.5f, 0.5f
        }

    type TextureConfig = {
        Columns: int
        Rows: int
        Tiling: bool
    } with
        static member Default = {
            Columns = 1
            Rows = 1
            Tiling = true
        }

    type Theme(storage) =

        member this.GetFile([<ParamArray>] path: string array) =
            let p = Path.Combine(path)
            try
                match storage with
                | Zip z -> z.GetEntry(p.Replace(Path.DirectorySeparatorChar, '/')).Open()
                | Folder f -> File.OpenRead(Path.Combine(f, p)) :> Stream
            with
            |  err -> reraise()

        member this.GetFiles([<ParamArray>] path: string array) =
            let p = Path.Combine(path)
            match storage with
            | Zip z ->
                let p = p.Replace(Path.DirectorySeparatorChar, '/')
                seq {
                    for e in z.Entries do
                        if e.FullName = p + "/" + e.Name && Path.HasExtension(e.Name) then yield e.Name
                }
            | Folder f -> Directory.EnumerateFiles(p)

        member this.GetFolders([<ParamArray>] path: string array) =
            let p = Path.Combine(path)
            match storage with
            | Zip z ->
                let p = p.Replace(Path.DirectorySeparatorChar, '/')
                seq {
                    for e in z.Entries do
                        if (e.Name = "" && e.FullName.Length > p.Length) then
                            let s = (e.FullName.Substring(p.Length + 1)).Split('/')
                            if e.FullName = p + "/" + s.[0] + "/" then yield s.[0]
                }
            | Folder f -> Directory.EnumerateDirectories(p)
        
        member this.GetJson([<ParamArray>] path: string array) =
            try
                use stream = this.GetFile(path)
                use tr = new StreamReader(stream)
                let json = tr.ReadToEnd() |> JsonHelper.load
                match storage with
                | Zip _ -> () //do not write data to zip archives
                | Folder f -> JsonHelper.saveFile json (Path.Combine(f, Path.Combine(path)))
                Some json
            with
            | err -> Logging.Debug("Defaulting on json file: " + String.concat "/" path) (err.ToString()); None

        member this.CopyTo(targetPath) =
            if Directory.Exists(targetPath) then
                match storage with
                | Zip z -> z.ExtractToDirectory(targetPath)
                | Folder f -> failwith "nyi, do this manually for now"
            else //copying to zip
                failwith "nyi, do this manually for now"