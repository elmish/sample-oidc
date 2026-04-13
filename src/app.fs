module App

(**
 - title: OIDC Sample
 - tagline: Elmish OIDC authentication with Microsoft Entra ID
*)

open System
open Fable.Core
open Fable.Core.JsInterop
open Elmish
open Elmish.OIDC
open Elmish.OIDC.Types

[<Literal>]
let ClientId = "YOUR_CLIENT_ID"

[<Literal>]
let TenantId = "YOUR_TENANT_ID"

let private baseUrl =
    let loc = Browser.Dom.window.location
    $"{loc.protocol}//{loc.host}{loc.pathname}".TrimEnd('/')

let oidcOptions : Options =
    { clientId = ClientId
      authority = $"https://login.microsoftonline.com/{TenantId}/v2.0"
      scopes = [ "openid"; "profile"; "email" ]
      redirectUri = baseUrl + "/"
      postLogoutRedirectUri = Some (baseUrl + "/")
      silentRedirectUri = Some (baseUrl + "/silent-renew.html")
      renewBeforeExpirySeconds = 60
      clockSkewSeconds = 300
      allowedAlgorithms = [ "RS256" ] }

type UserInfo =
    { name: string
      email: string }

let private getUserInfo (userinfoEndpoint: string) (accessToken: string) : Async<UserInfo> =
    Fetch.fetch userinfoEndpoint
        [ Fetch.requestHeaders [ Fetch.Types.Authorization $"Bearer {accessToken}" ] ]
    |> Promise.bind (fun resp -> resp.text())
    |> Promise.map (fun text ->
        let json = JS.JSON.parse text
        { name = json?name |> Option.ofObj |> Option.defaultValue (string json?sub)
          email = json?preferred_username |> Option.ofObj |> Option.defaultValue "" })
    |> Async.AwaitPromise

type Model =
    { oidc: Model<UserInfo> }

type Msg =
    | OidcMsg of Msg<UserInfo>

let init () =
    let oidcModel, oidcCmd = Oidc.init oidcOptions
    { oidc = oidcModel }, Cmd.map OidcMsg oidcCmd

let update (msg: Msg) (model: Model) =
    match msg with
    | OidcMsg m ->
        let m', c = Oidc.update oidcOptions getUserInfo m model.oidc
        { model with oidc = m' }, Cmd.map OidcMsg c

let subscribe (model: Model) =
    Oidc.subscribe model.oidc |> Sub.map "oidc" OidcMsg

open Fable.React
open Fable.React.Props

let private errorText (err: OidcError) =
    match err with
    | DiscoveryError ex -> $"Discovery failed: {ex.Message}"
    | IssuerMismatch (expected, actual) -> $"Issuer mismatch: expected {expected}, got {actual}"
    | InvalidState -> "Invalid state parameter (possible CSRF)"
    | TokenExchangeFailed msg -> $"Token exchange failed: {msg}"
    | InvalidToken msg -> $"Invalid token: {msg}"
    | Expired -> "Session expired"
    | ServerError (err, desc) -> $"Server error: {err} — {desc}"
    | NetworkError ex -> $"Network error: {ex.Message}"

let private viewLoading =
    div [ Class "container" ]
        [ div [ Class "loading" ] [ str "Initializing..." ] ]

let private viewError (err: OidcError) =
    div [ Class "container" ]
        [ div [ Class "error" ]
            [ h2 [] [ str "Authentication Error" ]
              p [] [ str (errorText err) ]
              p [] [ str "Check the browser console for details." ] ] ]

let private viewUnauthenticated dispatch =
    div [ Class "container" ]
        [ div [ Class "card" ]
            [ h1 [] [ str "Elmish OIDC Sample" ]
              p [] [ str "Sign in with your Microsoft account to continue." ]
              button
                [ Class "btn btn-primary"
                  OnClick (fun _ -> dispatch (OidcMsg LogIn)) ]
                [ str "Sign in with Microsoft" ] ] ]

let private viewAuthenticated (session: Session<UserInfo>) dispatch =
    let displayName =
        session.userInfo
        |> Option.map (fun u -> u.name)
        |> Option.defaultValue session.claims.sub

    let email =
        session.userInfo
        |> Option.map (fun u -> u.email)
        |> Option.defaultValue ""

    div [ Class "container" ]
        [ div [ Class "card" ]
            [ h1 [] [ str "Elmish OIDC Sample" ]
              div [ Class "user-info" ]
                [ h2 [] [ str $"Welcome, {displayName}!" ]
                  if email <> "" then
                      p [] [ str email ]
                  p [ Class "detail" ] [ str $"Subject: {session.claims.sub}" ]
                  p [ Class "detail" ] [ str $"Expires: {session.expiresAt.LocalDateTime}" ] ]
              button
                [ Class "btn btn-secondary"
                  OnClick (fun _ -> dispatch (OidcMsg LogOut)) ]
                [ str "Sign out" ] ] ]

let view (model: Model) (dispatch: Msg -> unit) =
    match model.oidc with
    | Initializing -> viewLoading
    | Failed err -> viewError err
    | Ready (_, _, readyState) ->
        match readyState with
        | Authenticated session
        | Renewing session ->
            viewAuthenticated session dispatch
        | ProcessingCallback _
        | ExchangingCode
        | ValidatingToken
        | Redirecting ->
            viewLoading
        | Unauthenticated ->
            viewUnauthenticated dispatch

open Elmish.React

Program.mkProgram init update view
|> Program.withSubscription subscribe
|> Program.withReactBatched "elmish-app"
|> Program.withConsoleTrace
|> Program.run

