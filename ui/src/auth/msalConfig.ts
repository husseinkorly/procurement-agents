export const msalConfig = {
  auth: {
    clientId: "91ae4f0e-1b07-4787-99a8-3901037fc809",
    authority: "https://login.microsoftonline.com/consumers",
    redirectUri: "https://green-grass-08fa61110.4.azurestaticapps.net",
    postLogoutRedirectUri: "https://green-grass-08fa61110.4.azurestaticapps.net",
    navigateToLoginRequestUrl: true
  },
  cache: {
    cacheLocation: 'localStorage',
    storeAuthStateInCookie: false
  },
};

export const loginRequest = {
  scopes: [
    "User.Read"
  ],
};