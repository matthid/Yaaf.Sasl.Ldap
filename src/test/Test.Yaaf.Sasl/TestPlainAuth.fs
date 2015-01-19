// ----------------------------------------------------------------------------
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.
// ----------------------------------------------------------------------------
namespace Test.Yaaf.Sasl

open System.IO
open NUnit.Framework
open FsUnit
open Yaaf.Sasl

[<TestFixture>]
type MechanismTest() = 
    
    let simpleSaslImpl (server : IServerMechanism) (client : IClientMechanism) = 
        let mutable clientContext = Unchecked.defaultof<_>
        let mutable serverContext = Unchecked.defaultof<_>
        let con, clientMsg = client.GetNextMessage(None)
        clientContext <- con
        let mutable serverMessage = Unchecked.defaultof<_>
        let con, servMsg = server.GetNextMessage(None, clientMsg)
        serverContext <- con
        serverMessage <- servMsg
        while serverMessage.IsChallengeMsg do
            let con, clientMsg = client.GetNextMessage(Some(clientContext, serverMessage))
            clientContext <- con
            let con, servMsg = server.GetNextMessage(Some serverContext, clientMsg)
            serverContext <- con
            serverMessage <- servMsg
        serverMessage
    
    member x.TestSuccessMechs (server : IServerMechanism) (client : IClientMechanism) = 
        let lastServerMessage = simpleSaslImpl server client
        lastServerMessage.IsSuccessMsg |> should be True
    
    member x.TestFailMechs (server : IServerMechanism) (client : IClientMechanism) = 
        let lastServerMessage = simpleSaslImpl server client
        lastServerMessage.IsFailureMsg |> should be True

[<TestFixture>]
type TestPlainAuth() = 
    inherit MechanismTest()
    
    static let getUserSource isAdmin username password = 
        { new Plain.IUserSource with
              member x.Authenticate(user, pass) = user = username && pass = password
              
              member x.Authorize(user, entity) = 
                  if isAdmin then true
                  else user = entity
              
              member x.DeriveAuthzid(user) = Some user }
    
    static member GetUserSource isAdmin username password = getUserSource isAdmin username password
    
    [<Test>]
    member this.``Check if plain works``() = 
        let userSource = getUserSource false "testuser" "testpass"
        let server = new Plain.PlainServer(userSource)
        let client = new Plain.PlainClient("", "testuser", "testpass")
        this.TestSuccessMechs server client
    
    [<Test>]
    member this.``Check if plain fails on invalid password``() = 
        let userSource = getUserSource false "testuser" "testpass"
        let server = new Plain.PlainServer(userSource)
        let client = new Plain.PlainClient("", "testuser", "testpdass")
        this.TestFailMechs server client
    
    [<Test>]
    member this.``Check if plain fails on no password``() = 
        let userSource = getUserSource false "testuser" "testpass"
        let server = new Plain.PlainServer(userSource)
        let client = new Plain.PlainClient("", "testuser", "")
        this.TestFailMechs server client
    
    [<Test>]
    member this.``Check if plain fails with invalid user``() = 
        let userSource = getUserSource false "testuser" "testpass"
        let server = new Plain.PlainServer(userSource)
        let client = new Plain.PlainClient("", "test#user", "")
        this.TestFailMechs server client
    
    [<Test>]
    member this.``Check if plain fails with no user``() = 
        let userSource = getUserSource false "testuser" "testpass"
        let server = new Plain.PlainServer(userSource)
        let client = new Plain.PlainClient("", "", "")
        this.TestFailMechs server client
    
    [<Test>]
    member this.``Check if plain fails with no user but password``() = 
        let userSource = getUserSource false "testuser" "testpass"
        let server = new Plain.PlainServer(userSource)
        let client = new Plain.PlainClient("", "", "asdas")
        this.TestFailMechs server client
    
    [<Test>]
    member this.``Check if plain fails with invalid authzid``() = 
        let userSource = getUserSource false "testuser" "testpass"
        let server = new Plain.PlainServer(userSource)
        let client = new Plain.PlainClient("test", "testuser", "testpass")
        this.TestFailMechs server client
