# MiniSlack Authentication Flow

MiniSlack uses Google OpenID Connect to verify the user's identity, then issues its own API tokens.

The browser keeps the long-lived refresh token in an `HttpOnly` cookie. Vue keeps the short-lived access token only in memory.

## Login Sequence

```mermaid
sequenceDiagram
    actor User
    participant Vue as Vue 3 App<br/>localhost:5173
    participant API as MiniSlack API<br/>https://localhost:7006
    participant Google as Google OIDC
    participant DB as PostgreSQL

    User->>Vue: Click "Continue with Google"
    Vue->>API: GET /auth/login/google
    API->>Google: Redirect to Google authorize endpoint
    Google->>User: Show Google sign-in / consent
    User->>Google: Sign in successfully
    Google->>API: GET /auth/callback/google<br/>with authorization code
    API->>Google: Exchange code and validate OIDC identity
    Google-->>API: ID/user claims<br/>sub, email, name, picture
    API->>DB: Find user by identity_provider + external_id
    alt First login
        API->>DB: Create local MiniSlack user
    else Returning user
        API->>DB: Update profile and last_login_at_utc
    end
    API->>DB: Store hashed refresh token
    API-->>Vue: Redirect to /auth/callback?login=success<br/>Set-Cookie: __Host-minislack_refresh
    Vue->>API: POST /auth/refresh<br/>credentials: include
    API->>DB: Validate refresh token hash
    API->>DB: Rotate refresh token
    API-->>Vue: accessToken + expiresInSeconds<br/>Set-Cookie: new refresh token
    Vue->>API: GET /auth/me<br/>Authorization: Bearer accessToken
    API-->>Vue: Current user profile
    Vue->>User: Show authenticated app
```

## Request Flow After Login

```mermaid
sequenceDiagram
    participant Vue as Vue 3 App
    participant API as MiniSlack API

    Vue->>API: Request protected endpoint<br/>Authorization: Bearer accessToken
    alt Access token is valid
        API-->>Vue: 200 OK
    else Access token expired
        API-->>Vue: 401 Unauthorized
        Vue->>API: POST /auth/refresh<br/>credentials: include
        API-->>Vue: New accessToken
        Vue->>API: Retry original request<br/>Authorization: Bearer new accessToken
        API-->>Vue: 200 OK
    end
```

## Logout Flow

```mermaid
sequenceDiagram
    participant Vue as Vue 3 App
    participant API as MiniSlack API
    participant DB as PostgreSQL

    Vue->>API: POST /auth/logout<br/>credentials: include
    API->>DB: Revoke refresh token
    API-->>Vue: 204 No Content<br/>Clear refresh cookie
    Vue->>Vue: Clear in-memory access token and user
    Vue->>Vue: Redirect to /login
```

## Token Storage

| Token | Stored In | Lifetime | Purpose |
| --- | --- | --- | --- |
| Google OIDC code/token | API only during callback | Very short | Verify Google identity |
| MiniSlack access token | Vue memory only | Short, e.g. 15 minutes | Authorize API calls |
| MiniSlack refresh token | `HttpOnly`, `Secure`, `SameSite=None`, `Path=/` cookie | Longer, e.g. 30 days | Get new access tokens |
| Refresh token hash | PostgreSQL | Until expiry/revocation | Validate refresh requests |

## Important Cookie Details

The refresh cookie is named:

```text
__Host-minislack_refresh
```

Because it uses the `__Host-` prefix, the cookie must be:

```text
Secure
Path=/
No Domain attribute
```

Because Vue runs on `http://localhost:5173` and the API runs on `https://localhost:7006`, refresh requests are cross-origin. The cookie also needs:

```text
SameSite=None
```

Vue must call refresh/logout endpoints with:

```ts
credentials: 'include'
```

## Why `/auth/refresh` Exists

The Google login callback does not send the access token in the URL. Instead:

1. The API sets the refresh cookie.
2. Vue lands on `/auth/callback?login=success`.
3. Vue calls `/auth/refresh`.
4. The API returns a short-lived access token.

This avoids putting tokens in browser history, logs, or referrer headers.

