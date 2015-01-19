// ----------------------------------------------------------------------------
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.
// ----------------------------------------------------------------------------

// NOTE: If warnings appear, you may need to retarget this project to .NET 4.0. Show the Solution
// Pad, right-click on the project node, choose 'Options --> Build --> General' and change the target
// framework to .NET 4.0 or .NET 4.5.

namespace Yaaf.Sasl

type Stream = System.IO.Stream
type RawData = byte[]

type SaslClientMessage =     
    | Auth of string * RawData option
    | Response of RawData
    | Abort
type SaslFailure = 
    /// The mechanism requested by the initiating entity cannot be used unless the confidentiality and integrity of the underlying stream are protected (typically via TLS); sent in reply to an <auth/> element (with or without initial response data). 
    | EncryptionRequired
    /// The authentication failed because the initiating entity provided credentials that have expired; sent in reply to a <response/> element or an <auth/> element with initial response data. 
    | CredentialsExpired
    /// The account of the initiating entity has been temporarily disabled; sent in reply to an <auth/> element (with or without initial response data) or a <response/> element. 
    | AccountDisabled
    /// The receiving entity acknowledges that the authentication handshake has been aborted by the initiating entity; sent in reply to the <abort/> element. 
    | Aborted
    /// The data provided by the initiating entity could not be processed because the base 64 encoding is incorrect (e.g., because the encoding does not adhere to the definition in Section 4 of [BASE64]); sent in reply to a <response/> element or an <auth/> element with initial response data. 
    | IncorrectEncoding
    /// The authzid provided by the initiating entity is invalid, either because it is incorrectly formatted or because the initiating entity does not have permissions to authorize that ID; sent in reply to a <response/> element or an <auth/> element with initial response data. 
    | InvalidAuthzid
    /// The initiating entity did not specify a mechanism, or requested a mechanism that is not supported by the receiving entity; sent in reply to an <auth/> element. 
    | InvalidMechanism
    /// The request is malformed (e.g., the <auth/> element includes initial response data but the mechanism does not allow that, or the data sent violates the syntax for the specified SASL mechanism); sent in reply to an <abort/>, <auth/>, <challenge/>, or <response/> element. 
    | MalformedRequest
    /// The mechanism requested by the initiating entity is weaker than server policy permits for that initiating entity; sent in reply to an <auth/> element (with or without initial response data). 
    | MechanismTooWeak
    /// The authentication failed because the initiating entity did not provide proper credentials, or because some generic authentication failure has occurred but the receiving entity does not wish to disclose specific information about the cause of the failure; sent in reply to a <response/> element or an <auth/> element with initial response data. 
    | NotAuthorized
    /// The authentication failed because of a temporary error condition within the receiving entity, and it is advisable for the initiating entity to try again later; sent in reply to an <auth/> element or a <response/> element. 
    | TemporaryAuthFailure
    | UnknownSaslFailure of string
type SaslServerMessage =
    | Challenge of RawData
    | Failure of SaslFailure * string option
    | Success of RawData option
    with 
        member x.IsChallengeMsg
            with get() =
                match x with
                | Challenge _ -> true
                | _ -> false
        member x.IsFailureMsg
            with get() =
                match x with
                | Failure _ -> true
                | _ -> false
        member x.IsSuccessMsg
            with get() =
                match x with
                | Success _ -> true
                | _ -> false

/// The Context of the Mechanism 
type IContext = interface end

type Context<'a> = {
    RawContext : 'a option }
    with 
        interface IContext
        static member Empty = { RawContext = None }:> IContext

type IClientMechanism =
    abstract member GetNextMessage : 
        (IContext * SaslServerMessage) option -> IContext * SaslClientMessage
    abstract member UpdateStream : IContext * RawData option -> Stream -> Stream
    abstract member Name : string with get

type IServerMechanism =
    abstract member GetNextMessage : 
        IContext option * SaslClientMessage -> IContext * SaslServerMessage

    abstract member UpdateStream : IContext -> Stream -> Stream
    abstract member GetAuthorizeId : IContext -> string
    abstract member Name : string with get









