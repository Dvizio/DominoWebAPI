import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { createLobby, joinLobby } from "../services/GameApi";

function LandingPage() {
    const navigate = useNavigate();
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

        setLoading(true);

        try {
            let result;

            if (gameId.trim()) {
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

            // Navigate to lobby page with the data
            navigate("/lobby", {
                state: {
                    lobby: result.lobby,
                    playerId: result.playerId
                }
            });

        } catch (err) {
            setError(err.message || "Failed to create/join game");
        } finally {
            setLoading(false);
        }
    }

    return (
        <div className="landing-page">
            <h1>Domino Game</h1>

            <form onSubmit={handleSubmit}>
                <div>
                    <label htmlFor="gameId">
                        Game ID
                    </label>

                    <input
                        id="gameId"
                        type="text"
                        value={gameId}
                        onChange={(event) =>
                            setGameId(event.target.value)
                        }
                        placeholder="Leave empty to create a game"
                    />
                </div>

                <div>
                    <label htmlFor="playerName">
                        Player Name
                    </label>

                    <input
                        id="playerName"
                        type="text"
                        value={playerName}
                        onChange={(event) =>
                            setPlayerName(event.target.value)
                        }
                        placeholder="Enter your name"
                    />
                </div>

                <button type="submit" disabled={loading}>
                    {loading
                        ? "Loading..."
                        : gameId.trim()
                            ? "Join Game"
                            : "Create Game"}
                </button>
            </form>

            {error && (
                <p className="error">
                    {error}
                </p>
            )}
        </div>
    );
}

export default LandingPage;