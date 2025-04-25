import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App.tsx';
import './index.css';
import { PublicClientApplication, EventType, AuthenticationResult } from '@azure/msal-browser';
import { MsalProvider } from '@azure/msal-react';
import { msalConfig } from './auth/msalConfig';

// Create MSAL instance
const msalInstance = new PublicClientApplication(msalConfig);

// Initialize the application
const initializeApp = async () => {
  try {
    // Initialize MSAL
    await msalInstance.initialize();

    // Handle redirect promise and set active account
    const response = await msalInstance.handleRedirectPromise();
    if (response) {
      msalInstance.setActiveAccount(response.account);
    }

    // Add event callback for login success
    msalInstance.addEventCallback((event) => {
      if (event.eventType === EventType.LOGIN_SUCCESS && event.payload) {
        const account = (event.payload as AuthenticationResult).account;
        msalInstance.setActiveAccount(account);
      }
    });

    // Render the application
    const root = document.getElementById('root');
    if (!root) throw new Error('Root element not found');

    ReactDOM.createRoot(root).render(
      <React.StrictMode>
        <MsalProvider instance={msalInstance}>
          <App />
        </MsalProvider>
      </React.StrictMode>
    );
  } catch (error) {
    console.error('Error initializing application:', error);
  }
};

// Start the application
initializeApp();
