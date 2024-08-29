import React, { useState, useEffect } from 'react';
import './App.css';

function App() {
    const [socket, setSocket] = useState(null);
    const [twitchUsername, setTwitchUsername] = useState("");
    const [gameData, setGameData] = useState({ health: 0, damage: 0, speed: 0 });

    useEffect(() => {
        if (twitchUsername) {
            const ws = new WebSocket(`wstwitch`);

            ws.onopen = () => {
                console.log('Connected to Website WebSocket');
            };

            ws.onmessage = (event) => {
                const data = JSON.parse(event.data);
                setGameData(data);
                console.log('Received game data:', data);
            };

            ws.onclose = () => {
                console.log('Disconnected from WebSocket');
            };

            setSocket(ws);
            return () => ws.close();
        }
    }, [twitchUsername]);

    const sendSpawnEnemyCommand = (enemyType) => {
        if (socket && socket.readyState === WebSocket.OPEN) {
            const message = {
                target: twitchUsername,
                spawnEnemy: enemyType
            };
            socket.send(JSON.stringify(message));
        }
    };

    return (
        <div>
            <h1 id="tableLabel">Game Control</h1>
            <p>Control the game via WebSocket</p>

            <input
                type="text"
                placeholder="Enter Twitch Username"
                value={twitchUsername}
                onChange={(e) => setTwitchUsername(e.target.value)}
            />
            <div>
                <p>Health: {gameData.health}</p>
                <p>Damage: {gameData.damage}</p>
                <p>Speed: {gameData.speed}</p>
            </div>
            <button onClick={() => sendSpawnEnemyCommand('enemyType1')}>Spawn Enemy 1</button>
            <button onClick={() => sendSpawnEnemyCommand('enemyType2')}>Spawn Enemy 2</button>
            <button onClick={() => sendSpawnEnemyCommand('enemyType3')}>Spawn Enemy 3</button>
        </div>
    );
}

export default App;