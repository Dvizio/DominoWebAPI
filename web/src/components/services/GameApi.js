const API_BASE_URL =
  import.meta.env.VITE_API_URL || "http://localhost:5170/api/games";
export const HUB_URL =
  import.meta.env.VITE_HUB_URL || "http://localhost:5170/gameHub";

console.log("API URL:", API_BASE_URL);
console.log("Hub URL:", HUB_URL);

export async function createLobby(hostPlayerName) {
  const response = await fetch(`${API_BASE_URL}/lobby`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ hostPlayerName }),
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || "Failed to create lobby");
  }

  return await response.json();
}

export async function joinLobby(gameId, playerName) {
  const response = await fetch(`${API_BASE_URL}/lobby/join`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ gameId, playerName }),
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || "Failed to join lobby");
  }

  return await response.json();
}

export async function updateSettings(settings) {
  const response = await fetch(`${API_BASE_URL}/lobby/settings`, {
    method: "PUT",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(settings),
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || "Failed to update settings");
  }

  return await response.json();
}

export async function startGame(gameId, playerId) {
  const response = await fetch(
    `${API_BASE_URL}/start?gameId=${encodeURIComponent(gameId)}&playerId=${encodeURIComponent(playerId)}`,
    {
      method: "POST",
    },
  );

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || "Failed to start game");
  }

  return await response.json();
}

export async function getGameState(gameId, playerId) {
  const response = await fetch(
    `${API_BASE_URL}/${encodeURIComponent(gameId)}?playerId=${encodeURIComponent(playerId)}`,
    {
      method: "GET",
    },
  );

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || "Failed to fetch game state");
  }

  return await response.json();
}

export async function playTile(gameId, playerId, tile, side) {
  const payload = {
    gameId,
    playerId: Number(playerId),
    tile: {
      left: Number(tile.left ?? tile.Left ?? 0),
      right: Number(tile.right ?? tile.Right ?? 0),
    },
    side: Number(side), // 0 for Left, 1 for Right
  };

  const response = await fetch(`${API_BASE_URL}/play`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(payload),
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || "Failed to play tile");
  }

  return await response.json();
}

export async function drawTile(gameId, playerId) {
  const response = await fetch(`${API_BASE_URL}/draw`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      gameId,
      playerId: Number(playerId),
    }),
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || "Failed to draw tile");
  }

  return await response.json();
}

export async function passTurn(gameId, playerId) {
  const response = await fetch(`${API_BASE_URL}/pass`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      gameId,
      playerId: Number(playerId),
    }),
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || "Failed to pass turn");
  }

  return await response.json();
}

export async function startNextRound(gameId, playerId) {
  const response = await fetch(
    `${API_BASE_URL}/next-round?gameId=${encodeURIComponent(gameId)}&playerId=${encodeURIComponent(playerId)}`,
    {
      method: "POST",
    },
  );

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || "Failed to start next round");
  }

  return await response.json();
}

export async function deleteLobby(gameId) {
  const response = await fetch(
    `${API_BASE_URL}/${encodeURIComponent(gameId)}`,
    {
      method: "DELETE",
    },
  );

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || "Failed to delete game lobby");
  }

  return await response.json();
}
