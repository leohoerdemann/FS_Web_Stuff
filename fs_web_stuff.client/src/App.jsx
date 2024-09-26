import React, { useEffect, useState } from 'react';
import './App.css';

function App() {
    const [streamerId, setStreamerId] = useState(null);
    const [viewerId, setViewerId] = useState(null);
    const [socket, setSocket] = useState(null);
    const [personalStamina, setPersonalStamina] = useState(100);
    const [sharedStamina, setSharedStamina] = useState(0);
    const [gameStarted, setGameStarted] = useState(false); // New state variable
    const maxPersonalStamina = 100;
    const maxSharedStamina = 100;

    useEffect(() => {
        // Function to extract query parameters from the URL
        const getQueryParams = () => {
            const params = new URLSearchParams(window.location.search);
            return {
                streamer: params.get('streamer'),
                viewer: params.get('viewer'),
            };
        };

        const { streamer, viewer } = getQueryParams();

        if (streamer && viewer) {
            setStreamerId(streamer);
            setViewerId(viewer);

            // Establish WebSocket connection
            const ws = new WebSocket(`wss://yourserver.com/ws/twitch?streamer=${encodeURIComponent(streamer)}&viewer=${encodeURIComponent(viewer)}`);

            ws.onopen = () => {
                console.log('WebSocket connected');
            };

            ws.onmessage = (event) => {
                const message = event.data;
                try {
                    const data = JSON.parse(message);
                    const command = data.command;

                    if (command === 'UPDATE_SHARED_STAMINA') {
                        setSharedStamina(data.stamina);
                    } else if (command === 'GAME_STARTED') {
                        setGameStarted(true);
                    } else if (command === 'GAME_STOPPED') {
                        setGameStarted(false);
                    }
                    // Handle other commands if necessary
                } catch (e) {
                    console.error('Error parsing message:', e);
                }
            };

            ws.onclose = () => {
                console.log('WebSocket disconnected');
            };

            setSocket(ws);

            return () => {
                ws.close();
            };
        } else {
            console.error('Streamer or viewer ID not found in URL parameters.');
        }
    }, []);

    // Personal stamina auto-regeneration
    useEffect(() => {
        if (gameStarted) { // Only regenerate stamina when game has started
            const regenInterval = setInterval(() => {
                setPersonalStamina((prev) => Math.min(prev + 1, maxPersonalStamina));
            }, 1000);

            return () => clearInterval(regenInterval);
        }
    }, [gameStarted]);

    const usePersonalStamina = (cost) => {
        if (personalStamina >= cost && socket && gameStarted) {
            setPersonalStamina(personalStamina - cost);

            const message = JSON.stringify({
                command: 'USE_PERSONAL_STAMINA',
                data: {
                    cost: cost,
                    viewer: viewerId,
                },
            });

            socket.send(message);
        }
    };

    const useSharedStamina = (cost) => {
        if (sharedStamina >= cost && socket && gameStarted) {
            const message = JSON.stringify({
                command: 'USE_SHARED_STAMINA',
                data: {
                    cost: cost,
                    viewer: viewerId,
                },
            });

            socket.send(message);
        }
    };

    if (!streamerId || !viewerId) {
        return (
            <div className="app-container">
                <p>Error: Streamer or viewer information not available.</p>
            </div>
        );
    }

    if (!gameStarted) {
        return (
            <div className="app-container">
                <p className="waiting-message">Waiting for game to start...</p>
            </div>
        );
    }

    return (
        <div className="app-container">
            <h5>Personal Stamina</h5>
            <div className="stamina-bar">
                <div
                    className="stamina-fill"
                    style={{ width: `${(personalStamina / maxPersonalStamina) * 100}%` }}
                ></div>
            </div>
            <div className="stamina-text">
                {personalStamina}/{maxPersonalStamina}
            </div>
            <div className="button-group">
                <button onClick={() => usePersonalStamina(10)} disabled={personalStamina < 10}>
                    Action 1 (-10)
                </button>
                <button onClick={() => usePersonalStamina(20)} disabled={personalStamina < 20}>
                    Action 2 (-20)
                </button>
                <button onClick={() => usePersonalStamina(30)} disabled={personalStamina < 30}>
                    Action 3 (-30)
                </button>
            </div>

            <h5>Shared Stamina</h5>
            <div className="stamina-bar">
                <div
                    className="stamina-fill shared"
                    style={{ width: `${(sharedStamina / maxSharedStamina) * 100}%` }}
                ></div>
            </div>
            <div className="stamina-text">
                {sharedStamina}/{maxSharedStamina}
            </div>
            <div className="button-group">
                <button onClick={() => useSharedStamina(15)} disabled={sharedStamina < 15}>
                    Group Action 1 (-15)
                </button>
                <button onClick={() => useSharedStamina(25)} disabled={sharedStamina < 25}>
                    Group Action 2 (-25)
                </button>
                <button onClick={() => useSharedStamina(35)} disabled={sharedStamina < 35}>
                    Group Action 3 (-35)
                </button>
            </div>
        </div>
    );
}

export default App;