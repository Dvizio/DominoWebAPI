import { BrowserRouter, Routes, Route } from "react-router-dom";
import LandingPage from "./components/pages/LandingPage";
import LobbyPage from "./components/pages/LobbyPage";
import GamePage from "./components/pages/GamePage";
import ErrorBoundary from "./components/common/ErrorBoundary";

function App() {
    return (
        <ErrorBoundary>
            <BrowserRouter>
                <Routes>
                    <Route path="/" element={<LandingPage />} />
                    <Route path="/lobby" element={<LobbyPage />} />
                    <Route path="/game/:gameId" element={<GamePage />} />
                </Routes>
            </BrowserRouter>
        </ErrorBoundary>
    );
}

export default App;