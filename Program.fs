// Learn more about F# at http://fsharp.org

open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open System.Diagnostics
open System.Reflection
open System.Collections.Generic
open System.Text.RegularExpressions

open Argu



type SparkConfig = {
    MicrosoftSparkVersion : string
    SparkVersion : string
    ProjectName : string
}
let ver = "3.5.6"
let script = "dotnet build\n
spark-submit --class org.apache.spark.deploy.dotnet.DotnetRunner --master local bin/Debug/netcoreapp3.0/microsoft-spark-2.4.x-0.6.0.jar dotnet bin/Debug/netcoreapp3.0/MySpark.dll"
let GetFirstFileMatch (files:string[]) : string= 
        match files.Length with
            | 0 -> ""
            | _ -> DirectoryInfo(files.[0]).Name
let RemoveExtension (x:string) = 
    x.Split(".").[0]

let ProcessOutput exe command =
    let p = new Process()
    p.StartInfo <- ProcessStartInfo("powershell.exe",command)
    p.StartInfo.UseShellExecute <- false
    p.StartInfo.CreateNoWindow <- true
    p.StartInfo.RedirectStandardOutput <- true
    p.Start() |> ignore
    p.WaitForExit() 
    p.StandardOutput.ReadToEnd()
    
let CreateConfig dir =
    let projName :string= Directory.GetFiles(dir, "*proj") 
                                |> GetFirstFileMatch
                                |> RemoveExtension
    let sparkV : string = 
        let output = ProcessOutput "powershell.exe" "spark-submit --version"
        let m = Regex.Match(output,"[0-9].[0-9].[0-9]")
        let v = if m.Success then m.Value.[0..m.Value.Length-3] else "No versions read"
        let files = Directory.GetFiles(Path.Join(dir,"/bin/debug/netcoreapp3.0/"),"*.jar")
        if files.[0].Contains(v+".x")  then files.[0] else files.[1]
    
    (projName, sparkV)



   
        

let InjectInfo ((pjName:string), (sparkV:string)) (command:string) : string =
    command.Replace("[pj]",pjName).Replace("[sv]",sparkV)
    

[<EntryPoint>]
let main argv =
    let currentDir = Environment.CurrentDirectory
    let config = "debug"
    let versions = 
        currentDir |> CreateConfig
    "spark-submit --class org.apache.spark.deploy.dotnet.DotnetRunner --master local [sv] dotnet bin\\Debug\\netcoreapp3.0\\[pj].dll"
        |> InjectInfo versions
        |> ProcessOutput "powershell.exe"
        |> printfn "%s"
    
    0 // return an integer exit code

    
