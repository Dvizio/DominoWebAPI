import { useState, useEffect, useRef } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { updateSettings, startGame, getGameState, HUB_URL } from "../services/GameApi";
import "./LobbyPage.css";

function LobbyPage() {
    const navigate = useNavigate();
    const location = useLocation();
    const connectionRef = useRef(null);
    const [copied, setCopied] = useState(false);
    
    // State initialization from location state or session storage
    const [lobby, setLobby] = useState(() => {
        const state = location.state;
        return state?.lobby || null;
    });
    const lobbyRef = useRef(lobby);
    useEffect(() => {
        lobbyRef.current = lobby;
    }, [lobby]);
    
    const [gameId] = useState(() => {
        const state = location.state;
        const gId = state?.lobby?.gameId || state?.gameId || sessionStorage.getItem("domino_current_gameId") || "";
        return gId ? gId.toUpperCase() : "";
    });

    const [playerId] = useState(() => {
        const state = location.state;
        const fromState = state?.playerId;
        if (fromState !== undefined && fromState !== null) {
            return Number(fromState);
        }
        const gId = (state?.lobby?.gameId || state?.gameId || sessionStorage.getItem("domino_current_gameId") || "").toUpperCase();
        if (gId) {
            const fromStorage = sessionStorage.getItem(`domino_player_${gId}`);
            if (fromStorage !== null) return Number(fromStorage);
        }
        return null;
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
                mode: state.lobby.mode ?? 1,
                deckSize: state.lobby.deckSize ?? 6,
                targetScore: state.lobby.targetScore ?? 100,
                handSize: state.lobby.handSize ?? 7,
                startingRule: state.lobby.startingRule ?? 0
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

    // Save gameId and playerId to sessionStorage whenever available
    useEffect(() => {
        if (gameId) {
            sessionStorage.setItem("domino_current_gameId", gameId);
        }
        if (gameId && playerId !== null) {
            sessionStorage.setItem(`domino_player_${gameId}`, String(playerId));
        }
    }, [gameId, playerId]);

    // Connect to SignalR hub and sync lobby state
    useEffect(() => {
        // Redirect if no game or player identification available
        if (!gameId || playerId === null) {
            navigate("/");
            return;
        }

        let isMounted = true;
        let hubConnection = null;

        const connectToHub = async () => {
            try {
                hubConnection = new HubConnectionBuilder()
                    .withUrl(HUB_URL)
                    .configureLogging(LogLevel.Information)
                    .withAutomaticReconnect()
                    .build();

                // Listen for lobby updates
                hubConnection.on("LobbyUpdated", (updatedLobby) => {
                    if (!isMounted || !updatedLobby) return;
                    console.log("Lobby updated:", updatedLobby);
                    setLobby(updatedLobby);
                    
                    // Keep settings in sync for all clients
                    setSettings({
                        mode: updatedLobby.mode ?? 1,
                        deckSize: updatedLobby.deckSize ?? 6,
                        targetScore: updatedLobby.targetScore ?? 100,
                        handSize: updatedLobby.handSize ?? 7,
                        startingRule: updatedLobby.startingRule ?? 0
                    });

                    // Update isHost status
                    const host = updatedLobby.players?.find(p => p.isHost);
                    setIsHost(host?.playerId === playerId);
                });

                // Listen for errors
                hubConnection.on("Error", (errorMessage) => {
                    if (!isMounted) return;
                    setError(errorMessage);
                });

                // Listen for game started
                hubConnection.on("GameStarted", () => {
                    if (!isMounted) return;
                    console.log("Game started! Transitioning to game table...");
                    sessionStorage.setItem(`domino_player_${gameId}`, String(playerId));
                    sessionStorage.setItem("domino_current_gameId", gameId);
                    navigate(`/game/${gameId}`, { 
                        state: { playerId, gameId, lobby: lobbyRef.current } 
                    });
                });

                // Listen for lobby closed
                hubConnection.on("LobbyClosed", (msg) => {
                    if (!isMounted) return;
                    alert(msg || "Game session has ended.");
                    navigate("/");
                });

                // Re-join room group automatically after reconnection
                hubConnection.onreconnected(async () => {
                    if (!isMounted) return;
                    console.log("Reconnected to SignalR hub, rejoining game group...");
                    setError("");
                    try {
                        await hubConnection.invoke("JoinGame", gameId, playerId);
                    } catch (rejoinErr) {
                        console.error("Error re-joining game group after reconnect:", rejoinErr);
                    }
                });

                hubConnection.onreconnecting(() => {
                    if (!isMounted) return;
                    setError("Connection lost, reconnecting...");
                });

                hubConnection.onclose(() => {
                    if (!isMounted) return;
                    console.log("Connection closed");
                });

                await hubConnection.start();
                
                if (!isMounted) {
                    await hubConnection.stop();
                    return;
                }

                connectionRef.current = hubConnection;
                console.log("Connected to SignalR hub in LobbyPage");
                setError("");
                
                // Join the SignalR room group for broadcasts
                try {
                    await hubConnection.invoke("JoinGame", gameId, playerId);
                } catch (joinError) {
                    console.error("Error joining game group:", joinError);
                }
                
            } catch (err) {
                if (isMounted) {
                    console.error("Error connecting to hub:", err);
                    setError("Failed to connect to game server");
                }
            }
        };

        // If we don't have initial lobby data (e.g. page refresh), fetch it first
        if (!lobby) {
            getGameState(gameId, playerId)
                .then(data => {
                    if (!isMounted) return;
                    if (data?.isActive || data?.game) {
                        navigate(`/game/${gameId}`, {
                            state: { playerId, gameId, lobby: data?.lobby }
                        });
                        return;
                    }
                    if (data?.lobby) {
                        setLobby(data.lobby);
                        const host = data.lobby.players?.find(p => p.isHost);
                        setIsHost(host?.playerId === playerId);
                        setSettings({
                            mode: data.lobby.mode ?? 1,
                            deckSize: data.lobby.deckSize ?? 6,
                            targetScore: data.lobby.targetScore ?? 100,
                            handSize: data.lobby.handSize ?? 7,
                            startingRule: data.lobby.startingRule ?? 0
                        });
                    }
                })
                .catch(err => {
                    console.error("Failed to load lobby state:", err);
                });
        }

        connectToHub();

        // Clean up connection on unmount
        return () => {
            isMounted = false;
            if (connectionRef.current) {
                connectionRef.current.stop().catch(() => {});
                connectionRef.current = null;
            } else if (hubConnection) {
                hubConnection.stop().catch(() => {});
            }
        };
        
    }, [gameId, playerId, navigate]);

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
            // Update via HTTP API
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
            // Start game via HTTP API (creates the game state on server and triggers SignalR broadcast)
            await startGame(gameId, playerId);

            sessionStorage.setItem(`domino_player_${gameId}`, String(playerId));
            sessionStorage.setItem("domino_current_gameId", gameId);

            // Navigate host to game page
            navigate(`/game/${gameId}`, { 
                state: { playerId, gameId, lobby: lobbyRef.current } 
            });
        } catch (err) {
            setError(err.message || "Failed to start game");
            setIsStarting(false);
        }
    };

    const handleCopyGameId = () => {
        navigator.clipboard.writeText(gameId);
        setCopied(true);
        setTimeout(() => setCopied(false), 2000);
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
        return (
            <div className="loading-lobby-screen">
                <h2>Loading Domino Table...</h2>
            </div>
        );
    }

    const modeName = settings.mode === 0 ? "Block Dominoes" : "Draw Dominoes";
    const deckName = settings.deckSize === 6 ? "Double 6 (28 tiles)" : settings.deckSize === 9 ? "Double 9 (55 tiles)" : "Double 12 (91 tiles)";
    const ruleName = settings.startingRule === 0 ? "Highest Double" : "Random Selection";

    return (
        <div className="lobby-container">
            {/* Header Bar */}
            <header className="lobby-header-bar">
                <div className="lobby-header-left">
                    <h1 className="lobby-title" ><a href ="https://lets-see-frrl.work">
                        Domino Table
                        </a>
                    </h1>
                    <span className="lobby-title-badge">Multiplayer Lounge</span>
                </div>

                <div className="lobby-header-right">
                    <div className="room-code-badge">
                        <span>Room Code:</span>
                        <strong>{gameId}</strong>
                    </div>

                    <button 
                        onClick={handleCopyGameId} 
                        className={`copy-code-btn ${copied ? "copied" : ""}`}
                        title="Copy game ID to clipboard"
                    >
                        {copied ? "✓ Copied!" : "Copy Code"}
                    </button>

                    <button onClick={handleLeaveLobby} className="leave-lobby-btn">
                        Exit Table
                    </button>
                </div>
            </header>

            {/* Main Content Grid */}
            <main className="lobby-main">
                {/* Left Card: Players */}
                <div className="lobby-card">
                    <div className="card-header">
                        <h2 className="card-title">
                            Seated Players
                        </h2>
                        <span className="player-count-tag">
                            {lobby.players.length} / 4 Players
                        </span>
                    </div>

                    <ul className="players-list">
                        {lobby.players.map(player => {
                            const isMe = player.playerId === playerId;
                            const initial = (player.playerName || "P")[0].toUpperCase();
                            return (
                                <li key={player.playerId} className={`player-item-card ${isMe ? "is-you" : ""}`}>
                                    <div className="player-item-left">
                                        <div className="player-avatar">
                                            {initial}
                                        </div>
                                        <span className="player-name-text">
                                            {player.playerName}
                                            {isMe && <span className="you-tag">YOU</span>}
                                        </span>
                                    </div>

                                    <div className="player-item-right">
                                        {player.isHost && (
                                            <span className="host-badge-pill">Host</span>
                                        )}
                                    </div>
                                </li>
                            );
                        })}
                    </ul>

                    {lobby.players.length < 2 && (
                        <div className="waiting-players-box">
                            <span>Waiting for players to join (Need at least 2 to start)</span>
                        </div>
                    )}
                </div>

                {/* Right Card: Game Settings */}
                <div className="lobby-card">
                    <div className="card-header">
                        <h2 className="card-title">
                            Table Rules & Settings
                        </h2>
                        {isHost && (
                            <span className="player-count-tag" style={{ color: "#86efac" }}>
                                Host Controls
                            </span>
                        )}
                    </div>

                    {isHost ? (
                        <form className="settings-form" onSubmit={(e) => { e.preventDefault(); handleUpdateSettings(); }}>
                            <div className="settings-grid">
                                <div className="settings-field">
                                    <label className="settings-label" htmlFor="mode">Game Mode</label>
                                    <select
                                        id="mode"
                                        name="mode"
                                        className="settings-select"
                                        value={settings.mode}
                                        onChange={handleSettingsChange}
                                        disabled={loading}
                                    >
                                        <option value={0}>Block Dominoes</option>
                                        <option value={1}>Draw Dominoes</option>
                                    </select>
                                </div>

                                <div className="settings-field">
                                    <label className="settings-label" htmlFor="deckSize">Deck Size</label>
                                    <select
                                        id="deckSize"
                                        name="deckSize"
                                        className="settings-select"
                                        value={settings.deckSize}
                                        onChange={handleSettingsChange}
                                        disabled={loading}
                                    >
                                        <option value={6}>Double 6 (28 tiles)</option>
                                        <option value={9}>Double 9 (55 tiles)</option>
                                        <option value={12}>Double 12 (91 tiles)</option>
                                    </select>
                                </div>

                                <div className="settings-field">
                                    <label className="settings-label" htmlFor="handSize">Hand Size</label>
                                    <input
                                        id="handSize"
                                        name="handSize"
                                        className="settings-input"
                                        type="number"
                                        min="1"
                                        max="15"
                                        value={settings.handSize}
                                        onChange={handleSettingsChange}
                                        disabled={loading}
                                    />
                                </div>

                                <div className="settings-field">
                                    <label className="settings-label" htmlFor="targetScore">Target Score (pts)</label>
                                    <input
                                        id="targetScore"
                                        name="targetScore"
                                        className="settings-input"
                                        type="number"
                                        min="50"
                                        max="500"
                                        step="50"
                                        value={settings.targetScore}
                                        onChange={handleSettingsChange}
                                        disabled={loading}
                                    />
                                </div>

                                <div className="settings-field" style={{ gridColumn: "1 / -1" }}>
                                    <label className="settings-label" htmlFor="startingRule">First Turn Rule</label>
                                    <select
                                        id="startingRule"
                                        name="startingRule"
                                        className="settings-select"
                                        value={settings.startingRule}
                                        onChange={handleSettingsChange}
                                        disabled={loading}
                                    >
                                        <option value={0}>Highest Double Player Starts</option>
                                        <option value={2}>Random Player Starts</option>
                                    </select>
                                </div>
                            </div>

                            <button
                                type="button"
                                onClick={handleUpdateSettings}
                                disabled={loading}
                                className="update-settings-btn"
                            >
                                {loading ? "Updating..." : "Save Table Settings"}
                            </button>
                        </form>
                    ) : (
                        <div className="settings-readonly-list">
                            <div className="readonly-item">
                                <span className="readonly-label">Game Mode</span>
                                <span className="readonly-val">{modeName}</span>
                            </div>
                            <div className="readonly-item">
                                <span className="readonly-label">Deck Size</span>
                                <span className="readonly-val">{deckName}</span>
                            </div>
                            <div className="readonly-item">
                                <span className="readonly-label">Starting Hand</span>
                                <span className="readonly-val">{settings.handSize} tiles</span>
                            </div>
                            <div className="readonly-item">
                                <span className="readonly-label">Target Score</span>
                                <span className="readonly-val">{settings.targetScore} pts</span>
                            </div>
                            <div className="readonly-item">
                                <span className="readonly-label">First Turn Rule</span>
                                <span className="readonly-val">{ruleName}</span>
                            </div>
                        </div>
                    )}
                </div>

                {/* Error Banner */}
                {error && (
                    <div className="lobby-error-banner">
                        ⚠️ {error}
                    </div>
                )}

                {/* Bottom Actions Card */}
                <div className="lobby-actions-card">
                    <div className="actions-info">
                        <h3 className="actions-status-title">
                            {isHost
                                ? lobby.players.length >= 2
                                    ? "Ready to Launch!"
                                    : "Waiting for at least 2 players..."
                                : "Waiting for table host..."}
                        </h3>
                        <p className="actions-status-subtitle">
                            {isHost
                                ? "All players will automatically transition to the table when you start."
                                : "The table host can configure settings and launch the match."}
                        </p>
                    </div>

                    {isHost && (
                        <button
                            onClick={handleStartGame}
                            disabled={isStarting || lobby.players.length < 2}
                            className="start-game-btn"
                        >
                            {isStarting ? "Launching..." : "🚀 Start Game Now"}
                        </button>
                    )}
                </div>
            </main>
        </div>
    );
}

export default LobbyPage;