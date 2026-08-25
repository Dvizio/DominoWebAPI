import { useState, useEffect, useRef } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { updateSettings, startGame } from "../services/GameApi";

function LobbyPage() {
    const navigate = useNavigate();
    const location = useLocation();
    const connectionRef = useRef(null);
    
    // Remove unused setters - we only need the values
    const [lobby, setLobby] = useState(() => {
        const state = location.state;
        return state?.lobby || null;
    });
    const [playerId] = useState(() => {
        const state = location.state;
        return state?.playerId || null;
    });
    const [gameId] = useState(() => {
        const state = location.state;
        return state?.lobby?.gameId || "";
    });
    const [isHost, setIsHost] = useState(() => {
        const state = location.state;
        if (state?.lobby && state?.playerId) {
            const host = state.lobby.players.find(p => p.isHost);
            return host?.playerId === state.playerId;
        }
        return false;
    });
    const [settings, setSettings] = useState(() => {
        const state = location.state;
        if (state?.lobby) {
            return {
                mode: state.lobby.mode,
                deckSize: state.lobby.deckSize,
                targetScore: state.lobby.targetScore,
                handSize: state.lobby.handSize,
                startingRule: state.lobby.startingRule
            };
        }
        return {
            mode: 1,
            deckSize: 6,
            targetScore: 100,
            handSize: 7,
            startingRule: 0
        };
    });
    const [error, setError] = useState("");
    const [loading, setLoading] = useState(false);
    const [isStarting, setIsStarting] = useState(false);

    useEffect(() => {
        // Redirect if no lobby data
        if (!lobby || !playerId) {
            navigate("/");
            return;
        }

        // Connect to SignalR hub when component mounts
        const connectToHub = async () => {
            try {
                const hubConnection = new HubConnectionBuilder()
                    .withUrl("http://localhost:5170/gamehub") // Update with your URL
                    .configureLogging(LogLevel.Information)
                    .build();

                // Listen for lobby updates
                hubConnection.on("LobbyUpdated", (updatedLobby) => {
                    console.log("Lobby updated:", updatedLobby);
                    setLobby(updatedLobby);
                    
                    // Update isHost status
                    const host = updatedLobby.players.find(p => p.isHost);
                    setIsHost(host?.playerId === playerId);
                });

                // Listen for errors
                hubConnection.on("Error", (errorMessage) => {
                    setError(errorMessage);
                });

                // Listen for game started
                hubConnection.on("GameStarted", () => {
                    console.log("Game started!");
                    navigate(`/game/${gameId}`, { 
                        state: { playerId, gameId } 
                    });
                });

                await hubConnection.start();
                connectionRef.current = hubConnection;
                console.log("Connected to SignalR hub");

                // Join the lobby group
                // await hubConnection.invoke("JoinLobby", gameId, "Player");
                
            } catch (err) {
                console.error("Error connecting to hub:", err);
                setError("Failed to connect to game server");
            }
        };

        connectToHub();

        // Clean up connection on unmount
        return () => {
            if (connectionRef.current) {
                connectionRef.current.stop();
                connectionRef.current = null;
            }
        };
     
    }, [lobby, playerId, gameId, navigate]); // We use connectionRef, not connection state

    const handleSettingsChange = (e) => {
        const { name, value } = e.target;
        setSettings(prev => ({
            ...prev,
            [name]: parseInt(value) || 0
        }));
    };

    const handleUpdateSettings = async () => {
        if (!isHost) {
            setError("Only the host can change settings");
            return;
        }

        setLoading(true);
        setError("");

        try {
            const updatedLobby = await updateSettings({
                gameId: gameId,
                mode: settings.mode,
                deckSize: settings.deckSize,
                targetScore: settings.targetScore,
                handSize: settings.handSize,
                startingRule: settings.startingRule
            });

            setLobby(updatedLobby);
            
            // Also notify via SignalR
            if (connectionRef.current) {
                await connectionRef.current.invoke("UpdateSettings", {
                    gameId: gameId,
                    mode: settings.mode,
                    deckSize: settings.deckSize,
                    targetScore: settings.targetScore,
                    handSize: settings.handSize,
                    startingRule: settings.startingRule
                });
            }

        } catch (err) {
            setError(err.message || "Failed to update settings");
        } finally {
            setLoading(false);
        }
    };

    const handleStartGame = async () => {
        if (!isHost) {
            setError("Only the host can start the game");
            return;
        }

        if (lobby.players.length < 2) {
            setError("Need at least 2 players to start the game");
            return;
        }

        setIsStarting(true);
        setError("");

        try {
            // Start game via HTTP API
            await startGame(gameId, playerId);
            
            // Also notify via SignalR
            if (connectionRef.current) {
                await connectionRef.current.invoke("StartGame", gameId, playerId);
            }

            // Navigate will happen via SignalR event "GameStarted"
            
        } catch (err) {
            setError(err.message || "Failed to start game");
            setIsStarting(false);
        }
    };

    const handleCopyGameId = () => {
        navigator.clipboard.writeText(gameId);
        alert("Game ID copied to clipboard!");
    };

    const handleLeaveLobby = () => {
        // Clean up connection before leaving
        if (connectionRef.current) {
            connectionRef.current.stop();
            connectionRef.current = null;
        }
        navigate("/");
    };

    // Show loading if lobby data isn't ready
    if (!lobby) {
        return <div className="loading">Loading lobby...</div>;
    }

    return (
        <div className="lobby-page">
            <div className="lobby-header">
                <h1>Game Lobby</h1>
                <div className="game-id-section">
                    <span className="game-id-label">Game ID:</span>
                    <span className="game-id">{gameId}</span>
                    <button onClick={handleCopyGameId} className="copy-btn">
                        📋 Copy
                    </button>
                    <button onClick={handleLeaveLobby} className="leave-btn">
                        Leave
                    </button>
                </div>
            </div>

            <div className="lobby-content">
                <div className="players-section">
                    <h2>Players ({lobby.players.length})</h2>
                    <ul className="players-list">
                        {lobby.players.map(player => (
                            <li key={player.playerId} className="player-item">
                                <span className="player-name">
                                    {player.playerName}
                                    {player.playerId === playerId && " (You)"}
                                </span>
                                {player.isHost && (
                                    <span className="host-badge">👑 Host</span>
                                )}
                            </li>
                        ))}
                    </ul>
                    {lobby.players.length < 2 && (
                        <p className="waiting-message">
                            Waiting for more players... (Need at least 2)
                        </p>
                    )}
                </div>

                {isHost && (
                    <div className="settings-section">
                        <h2>Game Settings</h2>
                        <form className="settings-form">
                            <div className="form-group">
                                <label htmlFor="mode">Game Mode</label>
                                <select
                                    id="mode"
                                    name="mode"
                                    value={settings.mode}
                                    onChange={handleSettingsChange}
                                    disabled={loading}
                                >
                                    <option value={0}>Draw</option>
                                    <option value={1}>Block</option>
                                    <option value={2}>All Fives</option>
                                </select>
                            </div>

                            <div className="form-group">
                                <label htmlFor="deckSize">Deck Size</label>
                                <select
                                    id="deckSize"
                                    name="deckSize"
                                    value={settings.deckSize}
                                    onChange={handleSettingsChange}
                                    disabled={loading}
                                >
                                    <option value={6}>Double 6 (28 tiles)</option>
                                    <option value={9}>Double 9 (55 tiles)</option>
                                    <option value={12}>Double 12 (91 tiles)</option>
                                </select>
                            </div>

                            <div className="form-group">
                                <label htmlFor="handSize">Hand Size</label>
                                <input
                                    id="handSize"
                                    name="handSize"
                                    type="number"
                                    min="1"
                                    max="15"
                                    value={settings.handSize}
                                    onChange={handleSettingsChange}
                                    disabled={loading}
                                />
                            </div>

                            <div className="form-group">
                                <label htmlFor="targetScore">Target Score</label>
                                <input
                                    id="targetScore"
                                    name="targetScore"
                                    type="number"
                                    min="50"
                                    max="500"
                                    step="50"
                                    value={settings.targetScore}
                                    onChange={handleSettingsChange}
                                    disabled={loading}
                                />
                            </div>

                            <div className="form-group">
                                <label htmlFor="startingRule">Starting Rule</label>
                                <select
                                    id="startingRule"
                                    name="startingRule"
                                    value={settings.startingRule}
                                    onChange={handleSettingsChange}
                                    disabled={loading}
                                >
                                    <option value={0}>Highest Double</option>
                                    <option value={1}>Highest Tile</option>
                                    <option value={2}>Random</option>
                                </select>
                            </div>

                            <button
                                type="button"
                                onClick={handleUpdateSettings}
                                disabled={loading}
                                className="update-settings-btn"
                            >
                                {loading ? "Updating..." : "Update Settings"}
                            </button>
                        </form>
                    </div>
                )}

                {error && (
                    <div className="error-message">{error}</div>
                )}

                <div className="actions-section">
                    {isHost ? (
                        <button
                            onClick={handleStartGame}
                            disabled={isStarting || lobby.players.length < 2}
                            className="start-game-btn"
                        >
                            {isStarting ? "Starting..." : "Start Game"}
                        </button>
                    ) : (
                        <p className="waiting-host">
                            Waiting for host to start the game...
                        </p>
                    )}
                </div>
            </div>
        </div>
    );
}

export default LobbyPage;