﻿module Prelude.Web

open System
open System.ComponentModel
open System.Net
open System.Net.Http
open System.Threading.Tasks
open Percyqaz.Json
open Prelude.Common

let wClient() =
    let w = new WebClient()
    w.Headers.Add("User-Agent", "Interlude")
    w

let client() =
    let w = new HttpClient()
    w.DefaultRequestHeaders.Add("User-Agent", "Interlude")
    w

let private downloadString (url: string, callback) =
    async {
        try
            use w = client()
            let! result = w.GetStringAsync url |> Async.AwaitTask
            callback result
            return true
        with :? HttpRequestException as err ->
            Logging.Error("Failed to get web data from " + url, err)
            return false
    }

let downloadJson<'T> (url, callback) =
    downloadString(url,
        fun s ->
            match JSON.FromString<'T> s with
            | Ok s -> callback s
            | Error err -> Logging.Error("Failed to parse json data from "+ url, err))

let downloadFile (url: string, target: string) : StatusTask =
    fun output ->
        async {
            let tcs = new TaskCompletionSource<unit>(url)
            let completed =
                new AsyncCompletedEventHandler(fun cs ce ->
                    if ce.UserState = (tcs :> obj) then
                        if ce.Error <> null then tcs.TrySetException(ce.Error) |> ignore
                        elif ce.Cancelled then tcs.TrySetCanceled() |> ignore
                        else tcs.TrySetResult () |> ignore)
            let prog = new DownloadProgressChangedEventHandler(fun _ e -> output(sprintf "Downloading.. %sMB/%sMB (%i%%)" "" "" e.ProgressPercentage))

            output("Waiting for download...")
            use w = wClient()
            w.DownloadFileCompleted.AddHandler completed
            w.DownloadProgressChanged.AddHandler prog
            w.DownloadFileAsync(new Uri(url), target, tcs)
            let! _ = tcs.Task |> Async.AwaitTask
            w.DownloadFileCompleted.RemoveHandler completed
            w.DownloadProgressChanged.RemoveHandler prog

            if isNull tcs.Task.Exception then
                return true
            else
                Logging.Error("Failed to download file from " + url, tcs.Task.Exception)
                return false
        }