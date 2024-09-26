import React, { useEffect, useState } from 'react';
import './App.css';

function App() {
    const [streamerId, setStreamerId] = useState(null);
    const [viewerId, setViewerId] = useState(null);
    const [socket, setSocket] = useState(null);
    const [personalStamina, setPersonalStamina] = useState(100);
    const [sharedStamina, setSharedStamina] = useState(0);
    const [gameStarted, setGameStarted] = useState(false);
    const [personalStaminaValues, setPersonalStaminaValues] = useState({});
    const [audienceStaminaValues, setAudienceStaminaValues] = useState({});
    const maxPersonalStamina = 100;
    const maxSharedStamina = 100;

    useEffect(() => {
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

            const ws = new WebSocket(`wss://bts.freakshowgame.com/ws/twitch?streamer=${encodeURIComponent(streamer)}&viewer=${encodeURIComponent(viewer)}`);

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
                    } else if (command === 'SET_SITE_VALUES') {
                        setPersonalStaminaValues(data.data.personalStaminaValues);
                        setAudienceStaminaValues(data.data.audienceStaminaValues);
                    }
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
        if (gameStarted) {
            const regenInterval = setInterval(() => {
                setPersonalStamina((prev) => Math.min(prev + 1, maxPersonalStamina));
            }, 1000);

            return () => clearInterval(regenInterval);
        }
    }, [gameStarted]);

    const usePersonalStamina = (size) => {
        const cost = personalStaminaValues[size];
        if (personalStamina >= cost && socket && gameStarted) {
            setPersonalStamina(personalStamina - cost);

            const message = JSON.stringify({
                command: 'USE_PERSONAL_STAMINA',
                data: {
                    cost: cost,
                    viewer: viewerId,
                    size: size,
                },
            });

            socket.send(message);
        }
    };

    const useSharedStamina = (size) => {
        const cost = audienceStaminaValues[size];
        if (sharedStamina >= cost && socket && gameStarted) {
            const message = JSON.stringify({
                command: 'USE_SHARED_STAMINA',
                data: {
                    cost: cost,
                    viewer: viewerId,
                    size: size,
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
                {Math.round((personalStamina / maxPersonalStamina) * 100)}%
            </div>
            <div className="button-group">
                <button onClick={() => usePersonalStamina('small')} disabled={personalStamina < personalStaminaValues.small}>
                    Action Small (-{personalStaminaValues.small || '...'})
                </button>
                <button onClick={() => usePersonalStamina('medium')} disabled={personalStamina < personalStaminaValues.medium}>
                    Action Medium (-{personalStaminaValues.medium || '...'})
                </button>
                <button onClick={() => usePersonalStamina('large')} disabled={personalStamina < personalStaminaValues.large}>
                    Action Large (-{personalStaminaValues.large || '...'})
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
                {Math.round((sharedStamina / maxSharedStamina) * 100)}%
            </div>
            <div className="button-group">
                <button onClick={() => useSharedStamina('small')} disabled={sharedStamina < audienceStaminaValues.small}>
                    Group Action Small (-{audienceStaminaValues.small || '...'})
                </button>
                <button onClick={() => useSharedStamina('medium')} disabled={sharedStamina < audienceStaminaValues.medium}>
                    Group Action Medium (-{audienceStaminaValues.medium || '...'})
                </button>
                <button onClick={() => useSharedStamina('large')} disabled={sharedStamina < audienceStaminaValues.large}>
                    Group Action Large (-{audienceStaminaValues.large || '...'})
                </button>
            </div>
        </div>
    );
}

export default App;