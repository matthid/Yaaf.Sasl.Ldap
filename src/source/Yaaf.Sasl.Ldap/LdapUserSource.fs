// ----------------------------------------------------------------------------
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.
// ----------------------------------------------------------------------------
namespace Yaaf.Sasl.Ldap
open Yaaf.Sasl
open Yaaf.Sasl.Plain

open Novell.Directory.Ldap
type LdapServer = 
    {
        HostName : string
        Port : int
        Ssl : bool
    }
type LdapUser = 
    {
        Name : string
        Password : string
    }
type LdapConfig = {
    Host : LdapServer
    User : LdapUser }
module LdapConn = 
    let setEng () = 
        System.Threading.Thread.CurrentThread.CurrentCulture <- System.Globalization.CultureInfo.InvariantCulture
        System.Threading.Thread.CurrentThread.CurrentUICulture <- System.Globalization.CultureInfo.InvariantCulture

    (* not implemented on mono...
        open System.DirectoryServices.Protocols
        let connect (config:LdapConfig) = 
            setEng()
            let con = new LdapConnection(sprintf "%s:%d" config.HostName config.HostPort)
            let options = con.SessionOptions
            options.ProtocolVersion <- 3
            options.SecureSocketLayer <- true
            
            con.AuthType <- AuthType.Basic
            let cred = new System.Net.NetworkCredential(config.UserName, config.Password)
            con.Credential <- cred
            // try
            //options.StartTransportLayerSecurity(null)
            //with
            con.Bind()
            if (options.SecureSocketLayer) then
                printfn "secure!"
            con*)

    
    let connectNovell (config:LdapConfig) = 
        setEng()
        //System.Net.ServicePointManager.ServerCertificateValidationCallback <- (fun _ _ _ _ -> true)
        let con = new LdapConnection()
        con.add_UserDefinedServerCertValidationDelegate (fun _ _ -> true)
        con.SecureSocketLayer <- config.Host.Ssl
        con.Connect(config.Host.HostName, config.Host.Port)
        con.Bind(config.User.Name, config.User.Password)
        if not con.Bound then
            raise <| LdapException("username or password invalid", 49, "Invalid credentials")
        con

type LdapUserSource(server:LdapServer, convertUserName) =
    new (server:LdapServer) = LdapUserSource(server, id)
    interface IUserSource with
        member x.Authenticate (authcid, passwd) = 
            let user = 
              { Name = convertUserName authcid
                Password = passwd }
            try 
                LdapConn.connectNovell { Host = server; User = user }
                    |> ignore

                true
            with
            | :? LdapException -> false
        member x.DeriveAuthzid authcid =
            // TODO: check some groups?
            Some authcid
            //authzid option
        member x.Authorize (authcid, authzid) =
            authcid = authzid
            // bool
    
