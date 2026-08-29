import React, { useState, useEffect, useRef, useCallback } from "react";
import { useParams, useLocation, useNavigate } from "react-router-dom";
import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import DominoTile from "../common/DominoTile";
import {
  getGameState,
  playTile,
  drawTile,
  passTurn,
  startNextRound,
  deleteLobby,
  HUB_URL,
} from "../services/GameApi";
import "./GamePage.css";

function GamePage() {
  const { gameId } = useParams();
  const location = useLocation();
  const navigate = useNavigate();

  // Retrieve playerId from router state or sessionStorage
  const [playerId] = useState(() => {
    const fromState = location.state?.playerId;
    if (fromState !== undefined && fromState !== null) {
      sessionStorage.setItem(`domino_player_${gameId}`, String(fromState));
      return Number(fromState);
    }
    const fromStorage = sessionStorage.getItem(`domino_player_${gameId}`);
    return fromStorage !== null ? Number(fromStorage) : null;
  });

  const [lobbyPlayers, setLobbyPlayers] = useState(() => {
    return location.state?.lobby?.players || [];
  });

  const [gameState, setGameState] = useState(null);
  const [selectedTile, setSelectedTile] = useState(null);
  const [validSides, setValidSides] = useState([]);
  const [error, setError] = useState("");
  const [actionLoading, setActionLoading] = useState(false);

  const connectionRef = useRef(null);
  const boardScrollRef = useRef(null);

  // Helper to get player name by id
  const getPlayerName = useCallback(
    (id) => {
      const numId = Number(id);
      const list = lobbyPlayers || [];
      const found = list.find((p) => Number(p?.playerId ?? p?.PlayerId) === numId);
      if (found && (found.playerName || found.PlayerName)) {
        return found.playerName || found.PlayerName;
      }
      return `Player ${numId}`;
    },
    [lobbyPlayers]
  );

  // Fetch full game state from REST
  const refreshGameState = useCallback(async () => {
    if (!gameId || playerId === null) return;
    try {
      const data = await getGameState(gameId, playerId);
      if (data) {
        const gameObj = data.game ?? data.Game ?? (data.yourHand || data.YourHand ? data : null);
        if (gameObj) {
          setGameState(gameObj);
        }
        const lobbyObj = data.lobby ?? data.Lobby;
        if (lobbyObj?.players) {
          setLobbyPlayers(lobbyObj.players);
        }
        setError("");
      }
    } catch (err) {
      console.error("Error fetching game state:", err);
      setError(err.message || "Failed to load game state");
    }
  }, [gameId, playerId]);

  // Check valid placement sides for a tile
  const calculateValidSides = useCallback(
    (tile, board) => {
      if (!tile) return [];
      const played = board || gameState?.playedBoard || gameState?.PlayedBoard || [];
      if (played.length === 0) {
        return [0]; // First tile opening move places to Left (side 0)
      }

      const firstTile = played[0];
      const lastTile = played[played.length - 1];

      const tLeft = Number(tile.left ?? tile.Left ?? 0);
      const tRight = Number(tile.right ?? tile.Right ?? 0);
      const boardLeft = Number(firstTile.left ?? firstTile.Left ?? 0);
      const boardRight = Number(lastTile.right ?? lastTile.Right ?? 0);

      const sides = [];
      if (tLeft === boardLeft || tRight === boardLeft) {
        sides.push(0); // 0: Left
      }
      if (tLeft === boardRight || tRight === boardRight) {
        sides.push(1); // 1: Right
      }
      return sides;
    },
    [gameState]
  );

  // Check if player has any playable tiles in hand
  const hasPlayableTiles = useCallback(() => {
    const hand = gameState?.yourHand || gameState?.YourHand || [];
    if (hand.length === 0) return false;
    const played = gameState?.playedBoard || gameState?.PlayedBoard || [];
    if (played.length === 0) return true;
    return hand.some((tile) => calculateValidSides(tile, played).length > 0);
  }, [gameState, calculateValidSides]);

  // SignalR connection setup
  useEffect(() => {
    if (!gameId || playerId === null) {
      navigate("/");
      return;
    }

    let isMounted = true;
    let hubConnection = null;

    const connectHub = async () => {
      try {
        hubConnection = new HubConnectionBuilder()
          .withUrl(HUB_URL)
          .configureLogging(LogLevel.Information)
          .withAutomaticReconnect()
          .build();

        hubConnection.on("GameStateUpdated", () => {
          if (!isMounted) return;
          console.log("SignalR GameStateUpdated received -> refreshing state");
          refreshGameState();
          setSelectedTile(null);
          setValidSides([]);
        });

        hubConnection.on("LobbyClosed", (msg) => {
          if (!isMounted) return;
          alert(msg || "Game table has been closed.");
          navigate("/");
        });

        hubConnection.on("Error", (msg) => {
          if (!isMounted) return;
          setError(msg);
        });

        hubConnection.onreconnected(async () => {
          if (!isMounted) return;
          console.log("SignalR Reconnected -> rejoining group");
          try {
            await hubConnection.invoke("JoinGame", gameId, playerId);
          } catch (e) {
            console.error("Rejoin group error:", e);
          }
          refreshGameState();
        });

        hubConnection.onreconnecting(() => {
          if (!isMounted) return;
          setError("Connection lost, reconnecting...");
        });

        hubConnection.onclose(() => {
          if (!isMounted) return;
          console.log("GameHub connection closed");
        });

        hubConnection.on("PlayerDisconnected", (msg) => {
          console.log("someone disconnected");
          console.log(getPlayerName(msg));
          refreshGameState();  //TODO langsung gamestateover?
        });

        hubConnection.on("LobbyUpdated" , (msg) => {
          console.log(msg)
        });

        await hubConnection.start();

        if (!isMounted) {
          await hubConnection.stop();
          return;
        }

        connectionRef.current = hubConnection;
        console.log("Connected to SignalR GameHub on GamePage");

        // Join the room group and pass playerId to register reconnection
        try {
          await hubConnection.invoke("JoinGame", gameId, playerId);
        } catch (e) {
          console.log("JoinGame invocation note:", e);
        }

        // Initial state fetch
        await refreshGameState();
      } catch (err) {
        console.error("Hub connection error:", err);
        if (isMounted) {
          setError("Failed to connect to real-time game updates");
          refreshGameState();
        }
      }
    };

    connectHub();

    return () => {
      isMounted = false;
      if (connectionRef.current) {
        connectionRef.current.stop().catch(() => { });
        connectionRef.current = null;
      } else if (hubConnection) {
        hubConnection.stop().catch(() => { });
      }
    };
  }, [gameId, playerId, navigate, refreshGameState]);

  // Scroll board into center view when playedBoard changes
  useEffect(() => {
    const played = gameState?.playedBoard || gameState?.PlayedBoard;
    if (boardScrollRef.current && played && played.length > 0) {
      boardScrollRef.current.scrollLeft =
        (boardScrollRef.current.scrollWidth - boardScrollRef.current.clientWidth) / 2;
    }
  }, [gameState?.playedBoard, gameState?.PlayedBoard]);

  // Execute playing a tile to a chosen side
  const executePlayTile = async (tile, side) => {
    if (!tile) return;
    setActionLoading(true);
    setError("");

    try {
      const updatedState = await playTile(gameId, playerId, tile, side);
      if (updatedState) {
        setGameState(updatedState);
      }
      setSelectedTile(null);
      setValidSides([]);
      // Refresh to ensure all client data is synchronized
      await refreshGameState();
    } catch (err) {
      console.error("Play tile error:", err);
      setError(err.message || "Failed to play tile");
    } finally {
      setActionLoading(false);
    }
  };

  // Handle tile click from player hand
  const handleSelectTile = (tile) => {
    const curPlayerId = Number(gameState?.currentPlayerId ?? gameState?.CurrentPlayerId);
    const myPlayerId = Number(playerId);
    const status = gameState?.status ?? gameState?.Status;

    if (curPlayerId !== myPlayerId) {
      setError(`It is not your turn (Waiting for ${getPlayerName(curPlayerId)}).`);
      return;
    }
    if (status !== "Playing") {
      setError("Game is not currently active.");
      return;
    }

    const tLeft = Number(tile.left ?? tile.Left ?? 0);
    const tRight = Number(tile.right ?? tile.Right ?? 0);
    const played = gameState?.playedBoard || gameState?.PlayedBoard || [];

    // Opening move on an empty board: auto-play immediately!
    if (played.length === 0) {
      executePlayTile(tile, 0);
      return;
    }

    const sides = calculateValidSides(tile, played);
    if (sides.length === 0) {
      const firstTile = played[0];
      const lastTile = played[played.length - 1];
      const boardLeft = Number(firstTile?.left ?? firstTile?.Left ?? 0);
      const boardRight = Number(lastTile?.right ?? lastTile?.Right ?? 0);
      setError(
        `Tile [${tLeft}|${tRight}] cannot match either end of the board (Ends are: ${boardLeft} and ${boardRight}).`
      );
      setSelectedTile(null);
      setValidSides([]);
      return;
    }

    // If only one side is valid, auto-play for instant placement!
    if (sides.length === 1) {
      executePlayTile(tile, sides[0]);
      return;
    }

    // If both Left and Right are valid:
    // If tile is already selected, clicking it again plays to Left
    if (
      selectedTile &&
      Number(selectedTile.left ?? selectedTile.Left) === tLeft &&
      Number(selectedTile.right ?? selectedTile.Right) === tRight
    ) {
      executePlayTile(tile, 0);
      return;
    }

    // Otherwise, select the tile and show Left / Right buttons on the board
    setError("");
    setSelectedTile(tile);
    setValidSides(sides);
  };

  // Handle drawing a tile
  const handleDrawTile = async () => {
    const curPlayerId = Number(gameState?.currentPlayerId ?? gameState?.CurrentPlayerId);
    if (curPlayerId !== Number(playerId)) return;

    setActionLoading(true);
    setError("");

    try {
      const updatedState = await drawTile(gameId, playerId);
      if (updatedState) {
        setGameState(updatedState);
      }
      await refreshGameState();
    } catch (err) {
      setError(err.message || "Cannot draw tile");
    } finally {
      setActionLoading(false);
    }
  };

  // Handle passing turn
  const handlePassTurn = async () => {
    const curPlayerId = Number(gameState?.currentPlayerId ?? gameState?.CurrentPlayerId);
    if (curPlayerId !== Number(playerId)) return;

    setActionLoading(true);
    setError("");

    try {
      const updatedState = await passTurn(gameId, playerId);
      if (updatedState) {
        setGameState(updatedState);
      }
      setSelectedTile(null);
      setValidSides([]);
      await refreshGameState();
    } catch (err) {
      setError(err.message || "Cannot pass turn");
    } finally {
      setActionLoading(false);
    }
  };

  // Handle starting the next round
  const handleStartNextRound = async () => {
    setActionLoading(true);
    setError("");

    try {
      const updatedState = await startNextRound(gameId, playerId);
      if (updatedState) {
        setGameState(updatedState);
      }
      setSelectedTile(null);
      setValidSides([]);
      await refreshGameState();
    } catch (err) {
      setError(err.message || "Failed to start next round");
    } finally {
      setActionLoading(false);
    }
  };

  const handleLeaveGame = async () => {
    if (connectionRef.current) {
      connectionRef.current.stop();
      connectionRef.current = null;
    }

    const currentStatus = gameState?.status ?? gameState?.Status;
    // If game is over, clean up the lobby
    if (currentStatus === "GameOver") {
      try {
        await deleteLobby(gameId);
      } catch (e) {
        console.log("Delete lobby note:", e);
      }
    }

    navigate("/");
  };

  if (!gameState) {
    return (
      <div className="game-loading-screen">
        <h2>Loading Domino Table...</h2>
        {error && <p className="error-message">{error}</p>}
      </div>
    );
  }

  // Normalize normalized properties from gameState
  const status = gameState.status ?? gameState.Status ?? "";
  const curPlayerId = Number(gameState.currentPlayerId ?? gameState.CurrentPlayerId);
  const myPlayerId = Number(playerId);
  const isMyTurn = curPlayerId === myPlayerId;
  const isPlaying = status === "Playing";
  const isRoundOver = status === "RoundOver";
  const isGameOver = status === "GameOver";
  const roundNum = gameState.roundNumber ?? gameState.RoundNumber ?? 1;
  const playedBoard = gameState.playedBoard ?? gameState.PlayedBoard ?? [];
  const yourHand = gameState.yourHand ?? gameState.YourHand ?? [];
  const deckCount = gameState.remainingDeckCount ?? gameState.RemainingDeckCount ?? 0;
  const otherHandCounts = gameState.otherPlayerHandCounts ?? gameState.OtherPlayerHandCounts ?? {};
  const scores = gameState.scores ?? gameState.Scores ?? {};
  const roundWinnerId = gameState.roundWinnerId ?? gameState.RoundWinnerId;
  const gameWinnerId = gameState.gameWinnerId ?? gameState.GameWinnerId;

  const canDraw = isMyTurn && isPlaying && deckCount > 0;
  const canPass = isMyTurn && isPlaying && !hasPlayableTiles();

  // Serpentine board row grouping
  const TILES_PER_ROW = 6;
  const snakeRows = [];
  if (playedBoard && playedBoard.length > 0) {
    for (let i = 0; i < playedBoard.length; i += TILES_PER_ROW) {
      const chunk = playedBoard.slice(i, i + TILES_PER_ROW);
      const rowIndex = Math.floor(i / TILES_PER_ROW);
      const isRtl = rowIndex % 2 === 1;
      snakeRows.push({
        rowIndex,
        isRtl,
        tiles: chunk,
        startIndex: i,
      });
    }
  }

  return (
    <div className="game-page-container">
      {/* Top Header Bar */}
      <header className="game-header">
        <div className="header-left">
          <span className="room-code-tag">
            Room: <strong>{gameId}</strong>
          </span>
          <span className="round-badge">Round #{roundNum}</span>
        </div>

        <div className="header-center">
          <span className="deck-counter">
            🀄 Deck: <strong>{deckCount}</strong> left
          </span>
        </div>

        <div className="header-right">
          <button className="leave-game-btn" onClick={handleLeaveGame}>
            Exit Game
          </button>
        </div>
      </header>

      {/* Opponents Ribbon (Top) */}
      <section className="opponents-section">
        {Object.entries(otherHandCounts).map(([opIdStr, tileCount]) => {
          const opId = Number(opIdStr);
          const isOpTurn = curPlayerId === opId;
          const opScore = scores[opId] ?? scores[opIdStr] ?? 0;

          return (
            <div
              key={opId}
              className={`opponent-card ${isOpTurn ? "active-turn" : ""}`}
            >
              <div className="opponent-header">
                <span className="opponent-name">{getPlayerName(opId)}</span>
                {isOpTurn && <span className="turn-tag">Thinking...</span>}
              </div>
              <div className="opponent-details">
                <span className="opponent-tiles">🀄 {tileCount} tiles</span>
                <span className="opponent-score">🏆 {opScore} pts</span>
              </div>
            </div>
          );
        })}
      </section>

      {/* Center Table / Domino Board */}
      <main className="game-board-table">
        <div className="board-scroll-container" ref={boardScrollRef}>
          {playedBoard.length > 0 ? (
            <div className="domino-snake-board">
              {snakeRows.map((row, rIdx) => {
                const isFirstRow = rIdx === 0;
                const isLastRow = rIdx === snakeRows.length - 1;
                const hasNextRow = rIdx < snakeRows.length - 1;
                const isRtl = row.isRtl;

                return (
                  <div key={row.rowIndex} className="snake-row-container">
                    <div className={`snake-row ${isRtl ? "row-rtl" : "row-ltr"}`}>
                      {/* Left End Placement Choice Button (Attached to Tile 0 at start of first row) */}
                      {isFirstRow && selectedTile && validSides.includes(0) && (
                        <button
                          className="placement-btn place-left-btn"
                          onClick={() => executePlayTile(selectedTile, 0)}
                          disabled={actionLoading}
                        >
                          ⬅ Place Left
                        </button>
                      )}

                      {/* Played Domino Chain Tiles */}
                      {row.tiles.map((tile, tileOffset) => {
                        const globalIdx = row.startIndex + tileOffset;
                        const tL = Number(tile?.left ?? tile?.Left ?? 0);
                        const tR = Number(tile?.right ?? tile?.Right ?? 0);
                        const isDouble = tL === tR;

                        return (
                          <div key={globalIdx} className="board-tile-wrapper">
                            <DominoTile
                              left={tL}
                              right={tR}
                              orientation={isDouble ? "vertical" : "horizontal"}
                              size="medium"
                              reverse={isRtl}
                            />
                          </div>
                        );
                      })}

                      {/* Right End Placement Choice Button (Attached to Tile N-1 at end of last row) */}
                      {isLastRow && selectedTile && validSides.includes(1) && (
                        <button
                          className="placement-btn place-right-btn"
                          onClick={() => executePlayTile(selectedTile, 1)}
                          disabled={actionLoading}
                        >
                          Place Right ➡
                        </button>
                      )}
                    </div>

                    {/* Corner connector to next row */}
                    {hasNextRow && (
                      <div className={`snake-turn-connector ${isRtl ? "turn-left" : "turn-right"}`}>
                        <div className="connector-curve" />
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          ) : (
            <div className="empty-board-placeholder">
              <p>The table is empty.</p>
              {isMyTurn ? (
                <p className="start-hint">
                  👉 Click any tile in your hand to make the opening move!
                </p>
              ) : (
                <p className="start-hint">
                  Waiting for {getPlayerName(curPlayerId)} to make the opening move...
                </p>
              )}
            </div>
          )}
        </div>
      </main>

      {/* Error / Notification Banner */}
      {error && <div className="game-error-banner">{error}</div>}

      {/* Bottom Player Dock */}
      <footer className="player-dock">
        <div className="dock-status-bar">
          <div className="player-identity">
            <span className="my-name">
              {getPlayerName(myPlayerId)} (You)
            </span>
            <span className="my-score">
              🏆 Score: {scores[myPlayerId] ?? scores[String(myPlayerId)] ?? 0}
            </span>
          </div>

          <div className="turn-banner">
            {isMyTurn ? (
              <span className="turn-indicator my-turn">
                🟢 Your Turn! Click a tile to play.
              </span>
            ) : (
              <span className="turn-indicator wait-turn">
                ⏳ Waiting for {getPlayerName(curPlayerId)}...
              </span>
            )}
          </div>

          <div className="dock-actions">
            <button
              className="action-btn draw-btn"
              onClick={handleDrawTile}
              disabled={!canDraw || actionLoading}
              title={
                !canDraw
                  ? "Cannot draw (deck empty or not your turn)"
                  : "Draw a tile from the boneyard"
              }
            >
              Draw Tile
            </button>
            <button
              className="action-btn pass-btn"
              onClick={handlePassTurn}
              disabled={!canPass || actionLoading}
              title={
                !canPass
                  ? "You have playable tiles or it is not your turn"
                  : "Pass your turn"
              }
            >
              Pass Turn
            </button>
          </div>
        </div>

        {/* Player's Hand */}
        <div className="player-hand-rack">
          {yourHand.length > 0 ? (
            yourHand.map((tile, idx) => {
              const tL = Number(tile.left ?? tile.Left ?? 0);
              const tR = Number(tile.right ?? tile.Right ?? 0);

              const isSelected =
                selectedTile &&
                Number(selectedTile.left ?? selectedTile.Left) === tL &&
                Number(selectedTile.right ?? selectedTile.Right) === tR;

              const sides = calculateValidSides(tile, playedBoard);
              const isPlayable = isMyTurn && isPlaying && sides.length > 0;

              return (
                <div key={idx} className="hand-tile-wrapper">
                  <DominoTile
                    left={tL}
                    right={tR}
                    orientation="vertical"
                    size="large"
                    selected={isSelected}
                    isPlayable={isPlayable}
                    disabled={!isMyTurn || !isPlaying || actionLoading}
                    onClick={() => handleSelectTile(tile)}
                  />
                </div>
              );
            })
          ) : (
            <div className="empty-hand">No tiles in hand</div>
          )}
        </div>
      </footer>

      {/* Round Over / Game Over Modal */}
      {(isRoundOver || isGameOver) && (
        <div className="modal-backdrop">
          <div className="summary-modal">
            <h2>{isGameOver ? "🎉 Game Over!" : "🏁 Round Finished!"}</h2>

            <div className="winner-highlight">
              {roundWinnerId ? (
                <p>
                  Winner: <strong>{getPlayerName(roundWinnerId)}</strong>
                </p>
              ) : (
                <p>Round ended in a tie!</p>
              )}
            </div>

            <table className="score-summary-table">
              <thead>
                <tr>
                  <th>Player</th>
                  <th>Current Score</th>
                </tr>
              </thead>
              <tbody>
                {Object.entries(scores).map(([pIdStr, score]) => {
                  const pId = Number(pIdStr);
                  const isWinner =
                    (isGameOver && Number(gameWinnerId) === pId) ||
                    (!isGameOver && Number(roundWinnerId) === pId);
                  return (
                    <tr key={pId} className={isWinner ? "winner-row" : ""}>
                      <td>
                        {getPlayerName(pId)}
                        {pId === myPlayerId && " (You)"}
                        {isWinner && " 🏆"}
                      </td>
                      <td>{score} pts</td>
                    </tr>
                  );
                })}
              </tbody>
            </table>

            <div className="modal-actions">
              {isGameOver ? (
                <button
                  className="modal-btn return-btn"
                  onClick={handleLeaveGame}
                >
                  Return to Home
                </button>
              ) : (
                <button
                  className="modal-btn next-round-btn"
                  onClick={handleStartNextRound}
                  disabled={actionLoading}
                >
                  {actionLoading ? "Starting..." : "Start Next Round"}
                </button>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default GamePage;
