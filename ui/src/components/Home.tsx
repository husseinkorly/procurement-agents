import { useState, useEffect, useRef } from 'react';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import copilotIcon from '../assets/Copilot_Icon.png';
import { Loader2 } from 'lucide-react';

// Add CSS for table styling
const tableStyles = `
  .table-container table {
    border-collapse: separate;
    border-spacing: 10px 4px;
    width: 100%;
    margin: 10px 0;
    font-size: 14px;
  }
  
  .table-container th {
    text-align: left;
    padding: 8px;
    border-bottom: 1px solid #ddd;
    font-weight: 600;
  }
  
  .table-container td {
    padding: 8px;
    min-width: 80px;
  }
`;

type Message = {
  content: string;
  isFromUser: boolean;
  html?: string;
  agentName?: string;
};

type SuggestedPrompt = {
  text: string;
};

// Backend API response types
interface MessageResponse {
  role: string;
  agentName: string;
  content: string;
  isHtml: boolean;
}

interface PromptResponse {
  messages: MessageResponse[];
}

export default function Home() {
  const [prompt, setPrompt] = useState<string>('');
  const [messages, setMessages] = useState<Message[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [showSuggestions, setShowSuggestions] = useState(true);
  const mainRef = useRef<HTMLDivElement>(null);

  const suggestedPrompts: SuggestedPrompt[] = [
    { text: 'List POs with invoices ready for review.' },
    { text: 'List all open POs ready for invoicing.' },
    { text: 'List all pending invoices for approval.' },
    { text: 'Create an invoice for PO number' },
    { text: 'Request an increase for my approval safe limit' },
    { text: 'Track status of purchase order #12345.' }
  ];

  useEffect(() => {
    if (mainRef.current && messages.length > 0) {
      requestAnimationFrame(() => {
        const element = mainRef.current;
        if (element) {
          element.scrollTop = element.scrollHeight;
        }
      });
    }
  }, [messages]);

  const sendPrompt = async () => {
    if (!prompt) return;
    setLoading(true);
    setErrorMessage(null);
    setShowSuggestions(false);
    
    const userMessage = { content: prompt, isFromUser: true };
    setMessages(prev => [...prev, userMessage]);
    
    try {
      // Call the backend API using the correct port 5000
      const response = await fetch(
        'http://localhost:5000/api/chat/prompt',
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ prompt }),
        }
      );

      if (!response.ok) {
        throw new Error(`Server responded with status: ${response.status}`);
      }

      const data = await response.json() as PromptResponse;
      
      // Process each message from the agents
      data.messages.forEach(msg => {
        setMessages(prev => [...prev, {
          content: msg.content,
          isFromUser: false,
          html: msg.isHtml ? msg.content : undefined,
          agentName: msg.agentName
        }]);
      });
      
      setPrompt('');
    } catch (error) {
      console.error('Error sending prompt', error);
      setErrorMessage(
        'An error occurred while processing your request. Please try again.'
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className='flex flex-col h-screen bg-gradient-to-b from-gray-100 to-orange-100 text-black'>
      {/* Add style tag for table styling */}
      <style dangerouslySetInnerHTML={{ __html: tableStyles }} />
      
      {/* Title Section */}
      <header className='flex justify-center pt-16 pb-8'>
        <h1 className='text-2xl font-bold text-gray-800 font-["Segoe UI"] max-w-xl text-center'>
          Welcome to Procurement Copilot
        </h1>
      </header>

      {/* Main Content */}
      <main
        ref={mainRef}
        className='flex-1 overflow-y-auto p-4 pb-40 space-y-8 flex flex-col items-center'
      >
        {errorMessage && (
          <div className='w-full max-w-md p-4 bg-red-50 border border-red-100 rounded-lg text-sm text-gray-700 whitespace-pre-line'>
            {errorMessage}
          </div>
        )}
        
        {/* Chat messages */}
        <div className='w-full max-w-xl space-y-4'>
          {messages.map((message, index) => (
            <div
              key={index}
              className={`p-4 rounded-lg ${
                message.isFromUser
                  ? 'bg-blue-100 ml-auto max-w-[80%]'
                  : message.html && message.html.includes('<table')
                    ? 'bg-white w-full flex flex-col items-center'
                    : 'bg-white mr-auto max-w-[80%]'
              }`}
            >
              {message.agentName && !message.isFromUser && (
                <div className={`font-semibold text-sm text-gray-600 mb-2 ${
                  message.html && message.html.includes('<table') ? 'self-start' : ''
                }`}>
                  {message.agentName}
                </div>
              )}
              
              {message.html ? (
                <div className={`${
                  message.html.includes('<table') ? 'table-container' : 'w-full'
                }`} dangerouslySetInnerHTML={{ __html: message.html }} />
              ) : (
                <div>{message.content}</div>
              )}
            </div>
          ))}
        </div>
        
        {/* Loading indicator */}
        {loading && (
          <div className='flex items-center justify-center w-full'>
            <Loader2 className='h-8 w-8 animate-spin text-gray-500' />
          </div>
        )}
      </main>

      {/* Suggested Prompts Section */}
      {showSuggestions && (
        <div className='fixed bottom-48 left-0 right-0 flex justify-center'>
          <div className='w-full max-w-xl flex flex-col items-center gap-4 px-4'>
            <div className='grid grid-cols-2 gap-3 w-full'>
              {suggestedPrompts.map((suggestion, index) => (
                <button
                  key={index}
                  onClick={() => {
                    setPrompt(suggestion.text);
                  }}
                  className='p-3 bg-white rounded-lg shadow-md hover:shadow-lg transition-shadow
                            border border-gray-200 text-left text-gray-700 hover:bg-gray-50
                            flex items-center text-sm min-h-[60px]'
                >
                  {suggestion.text}
                </button>
              ))}
            </div>
          </div>
        </div>
      )}

      {/* Input Section */}
      <footer className='p-4 fixed bottom-16 left-0 right-0 flex justify-center z-10'>
        <div className='w-full max-w-xl flex gap-2 p-2 rounded-full border border-gray-300 bg-white shadow-lg'>
          <div className='flex items-center pl-3'>
            <img
              src={copilotIcon}
              alt='Copilot Icon'
              className='w-6 h-6 object-contain'
            />
          </div>
          <Input
            className='flex-1 bg-transparent text-black border-0 focus-visible:ring-0 focus-visible:ring-offset-0 focus:outline-none shadow-none'
            placeholder='Message Copilot'
            value={prompt}
            onChange={(e) => setPrompt(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendPrompt();
              }
            }}
          />
          <Button
            onClick={sendPrompt}
            disabled={loading}
            className='w-24 bg-gray-200 text-black hover:bg-gray-300 rounded-full'
          >
            {loading ? <Loader2 className='h-4 w-4 animate-spin' /> : 'Send'}
          </Button>
        </div>
      </footer>
    </div>
  );
}
