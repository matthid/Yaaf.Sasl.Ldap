// ----------------------------------------------------------------------------
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.
// ----------------------------------------------------------------------------

namespace Yaaf.Sasl.External
open Yaaf.Sasl
open System

type Authzid = string
type Authcid = string
type Passwd = string
type IExternalCheck =
    /// Empty means the external subject wants to Authorize as itself
    /// Nonempty means the Authcid wants to authorize as the given Authzid
    abstract member Authorize : Authzid option -> bool
    abstract member Authenticate : unit -> bool
type ExternalContext = {
    Authorization : string
}

type Context = Context<ExternalContext>

type ExternalServer(source:IExternalCheck) =
    let name = "EXTERNAL"
    let initResponse data =
        let authzidData = System.Text.Encoding.UTF8.GetString data     
        let authOk = source.Authenticate()
        let authzid = 
            if authzidData = "" then
                None
            else Some authzidData
        let authorizedOk = source.Authorize(authzid)
        
        if authOk && authorizedOk then
            { RawContext = Some {Authorization = authzid.Value}} :> IContext, Success None
        else
            if authOk && not authorizedOk then
                Context.Empty, Failure (SaslFailure.InvalidAuthzid, None)
            else
                Context.Empty, Failure (SaslFailure.NotAuthorized, None)
            
    interface IServerMechanism with
        member x.GetNextMessage (context, clientMessage) =
            match clientMessage with
            | Abort -> Context.Empty, Failure(SaslFailure.Aborted, None)
            | Auth (plain, initData) -> 
                assert (plain = name)
                match initData with
                | Some data -> initResponse data
                | None ->
                    // empty challenge to get initial data
                    Context.Empty, Challenge [||]
            | Response data -> 
                match context with
                | None -> failwith "Use auth messagefirst"
                | Some context -> 
                    initResponse data
            
        member x.UpdateStream context oldStream = 
            oldStream // No stream changes in PLAIN mech
            
        member x.GetAuthorizeId context = 
            let con = context :?> Context
            match con.RawContext with
            | Some c -> c.Authorization
            | None -> failwith "not authorized"

        member x.Name with get () = name
type ExternalClient (authzid:string option) =
    let authData = 
        let string = 
            match authzid with
            | Some auth -> auth
            | None -> ""
        System.Text.Encoding.UTF8.GetBytes (string)
    let name = "EXTERNAL"
    interface IClientMechanism with            
        member x.GetNextMessage oldData =
            match oldData with
            | None ->
                Context.Empty, Auth(name, Some authData)
            | Some (context, server) ->
                failwith "should not happen (success or failure expected)"
        member x.UpdateStream (context, data) stream = 
            stream // No stream changes in EXTERNAL mech
        member x.Name with get() = name












