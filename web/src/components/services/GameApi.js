const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

console.log("API URL:", API_BASE_URL);

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
    `${API_BASE_URL}/start?gameId=${gameId}&playerId=${playerId}`,
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
