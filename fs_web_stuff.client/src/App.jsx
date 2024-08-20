import { useEffect, useState } from 'react';
import './App.css';

function App() {
    const [playerCounts, setPlayerCounts] = useState();

    useEffect(() => {
        populatePlayerCounts();
    }, []);

    const contents = playerCounts === undefined
        ? <p><em>Loading... Please refresh once the server has started.</em></p>
        : <table className="table table-striped" aria-labelledby="tableLabel">
            <thead>
                <tr>
                    <th>Region</th>
                    <th>Player Count</th>
                </tr>
            </thead>
            <tbody>
                {Object.entries(playerCounts).map(([region, playerCount]) => (
                    <tr key={region}>
                        <td>{region}</td>
                        <td>{playerCount}</td>
                    </tr>
                ))}
            </tbody>
        </table>;

    return (
        <div>
            <h1 id="tableLabel">Player Counts</h1>
            <p>Freak show demo of player counts</p>
            {contents}
        </div>
    );
    
    async function populatePlayerCounts() {
        const response = await fetch('playercounts');
        const data = await response.json();
        setPlayerCounts(data);
    }
}

export default App;