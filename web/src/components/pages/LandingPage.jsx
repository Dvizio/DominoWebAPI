import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { createLobby, joinLobby } from "../services/GameApi";
import "./LandingPage.css";

function LandingPage() {
    const navigate = useNavigate();
    const [mode, setMode] = useState("create"); // "create" | "join"
    const [gameId, setGameId] = useState("");
    const [playerName, setPlayerName] = useState("");
    const [error, setError] = useState("");
    const [loading, setLoading] = useState(false);

    async function handleSubmit(event) {
        event.preventDefault();
        setError("");

        if (!playerName.trim()) {
            setError("Please enter your player name.");
            return;
        }

        if (mode === "join" && !gameId.trim()) {
            setError("Please enter a valid Game ID to join.");
            return;
        }

        setLoading(true);

        try {
            let result;

            if (mode === "join") {
                // Join existing lobby
                result = await joinLobby(
                    gameId.trim(),
                    playerName.trim()
                );
            } else {
                // Create new lobby
                result = await createLobby(
                    playerName.trim()
                );
            }

            console.log("Game result:", result);

            if (result?.lobby?.gameId && result?.playerId) {
                sessionStorage.setItem("domino_current_gameId", result.lobby.gameId);
                sessionStorage.setItem(`domino_player_${result.lobby.gameId}`, String(result.playerId));
            }

            // Navigate to lobby page with the data
            navigate("/lobby", {
                state: {
                    lobby: result.lobby,
                    playerId: result.playerId,
                    gameId: result.lobby.gameId
                }
            });

        } catch (err) {
            setError(err.message || "Failed to create or join game");
        } finally {
            setLoading(false);
        }
    }

    return (
        <div className="landing-container">
            <div className="landing-card">
                {/* Brand Header */}
                <div className="brand-header">
                    <div className="brand-logo-dominoes">
                        <div className="mini-domino">
                            <div className="mini-domino-half">
                                <span className="mini-dot" />
                                <span className="mini-dot" />
                                <span className="mini-dot" />
                            </div>
                            <div className="mini-divider" />
                            <div className="mini-domino-half">
                                <span className="mini-dot" />
                                <span className="mini-dot" />
                                <span className="mini-dot" />
                            </div>
                        </div>
                        <div className="mini-domino">
                            <div className="mini-domino-half">
                                <span className="mini-dot" />
                                <span className="mini-dot" />
                            </div>
                            <div className="mini-divider" />
                            <div className="mini-domino-half">
                                <span className="mini-dot" />
                                <span className="mini-dot" />
                                <span className="mini-dot" />
                                <span className="mini-dot" />
                            </div>
                        </div>
                    </div>
                    <span className="brand-badge">Premium Tabletop</span>
                    <h1 className="brand-title">Domino <span>Lounge</span></h1>
                    <p className="brand-subtitle">Real-time multiplayer domino gaming</p>
                </div>

                {/* Mode Switcher Tabs */}
                <div className="mode-tabs">
                    <button
                        type="button"
                        className={`mode-tab ${mode === "create" ? "active" : ""}`}
                        onClick={() => {
                            setMode("create");
                            setError("");
                        }}
                    >
                        ✨ Create Lobby
                    </button>
                    <button
                        type="button"
                        className={`mode-tab ${mode === "join" ? "active" : ""}`}
                        onClick={() => {
                            setMode("join");
                            setError("");
                        }}
                    >
                        🚪 Join Room
                    </button>
                </div>

                {/* Form */}
                <form className="landing-form" onSubmit={handleSubmit}>
                    <div className="form-field">
                        <label className="form-label" htmlFor="playerName">
                            Player Name
                        </label>
                        <div className="input-wrapper">
                            <span className="input-icon">👤</span>
                            <input
                                id="playerName"
                                className="form-input"
                                type="text"
                                value={playerName}
                                onChange={(e) => setPlayerName(e.target.value)}
                                placeholder="Enter your display name"
                                maxLength={20}
                                autoFocus
                            />
                        </div>
                    </div>

                    {mode === "join" && (
                        <div className="form-field">
                            <label className="form-label" htmlFor="gameId">
                                Room Code / Game ID
                            </label>
                            <div className="input-wrapper">
                                <span className="input-icon">🔑</span>
                                <input
                                    id="gameId"
                                    className="form-input"
                                    type="text"
                                    value={gameId}
                                    onChange={(e) => setGameId(e.target.value)}
                                    placeholder="Enter 6-character room code"
                                />
                            </div>
                        </div>
                    )}

                    <button className="submit-btn" type="submit" disabled={loading}>
                        {loading ? (
                            "Connecting..."
                        ) : mode === "create" ? (
                            "Create New Table 🎲"
                        ) : (
                            "Join Table 🀄"
                        )}
                    </button>
                </form>

                {error && (
                    <div className="landing-error">
                        <span>⚠️</span> {error}
                    </div>
                )}

                <div className="landing-footer">
                    <span>Block & Draw Rules • Up to 4 Players</span>
                </div>
            </div>
        </div>
    );
}

export default LandingPage;