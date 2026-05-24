# Vue 3 Authentication Flow

MiniSlack uses Google OpenID Connect only to verify the user's identity. After Google login, the API creates or updates the local MiniSlack user and issues MiniSlack tokens.

## Backend Endpoints

- `GET /auth/login/google`
  - Starts the Google OpenID Connect login flow.
- `GET /auth/signed-in/google`
  - Internal post-OIDC endpoint. It sets the refresh-token cookie and redirects to the Vue callback route.
- `POST /auth/refresh`
  - Reads the refresh-token cookie and returns a new short-lived access token.
- `POST /auth/logout`
  - Revokes the refresh token and clears the cookie.
- `GET /auth/me`
  - Returns the authenticated user's profile. Requires `Authorization: Bearer <accessToken>`.

## Vue Login Flow

1. Send the browser to the API login endpoint:

   ```ts
   window.location.href = `${apiBaseUrl}/auth/login/google`;
   ```

2. Google redirects back to the API.

3. The API sets an `HttpOnly` refresh-token cookie and redirects to:

   ```text
   http://localhost:5173/auth/callback?login=success
   ```

4. The Vue callback route calls `/auth/refresh` with cookies included:

   ```ts
   const response = await fetch(`${apiBaseUrl}/auth/refresh`, {
     method: 'POST',
     credentials: 'include'
   });

   const { accessToken, expiresInSeconds } = await response.json();
   ```

5. Store the access token in memory, not `localStorage`.

6. Send API requests with:

   ```ts
   Authorization: `Bearer ${accessToken}`
   ```

7. When the access token expires, call `/auth/refresh` again.

## Local Configuration

Set these values outside committed config, for example with user-secrets:

```bash
dotnet user-secrets set "Authentication:Google:ClientId" "<google-client-id>" --project MiniSlack.Api
dotnet user-secrets set "Authentication:Google:ClientSecret" "<google-client-secret>" --project MiniSlack.Api
dotnet user-secrets set "Authentication:Jwt:SigningKey" "<long-random-secret-at-least-32-bytes>" --project MiniSlack.Api
```

In Google Cloud Console, register this redirect URI:

```text
https://localhost:<api-port>/auth/callback/google
```

For Vue dev server defaults, keep this frontend origin:

```text
http://localhost:5173
```
