import React from "react";
import "./DominoTile.css";

// Standard pip positions in a 3x3 (for 0-9) or 3x4 grid (for 10-12)
function renderPips(value) {
  const count = Math.max(0, Math.min(12, Number(value) || 0));

  if (count === 0) {
    return <div className="pip-grid pip-grid-0" />;
  }

  // 1 to 9 use a 3x3 grid
  if (count <= 9) {
    const activeIndices = {
      1: [4], // center
      2: [0, 8], // top-left, bottom-right
      3: [0, 4, 8], // top-left, center, bottom-right
      4: [0, 2, 6, 8], // 4 corners
      5: [0, 2, 4, 6, 8], // 4 corners + center
      6: [0, 2, 3, 5, 6, 8], // 2 columns of 3
      7: [0, 2, 3, 4, 5, 6, 8], // 6 + center
      8: [0, 1, 2, 3, 5, 6, 7, 8], // 8 outer dots
      9: [0, 1, 2, 3, 4, 5, 6, 7, 8], // all 9
    }[count] || [];

    return (
      <div className="pip-grid pip-grid-3x3">
        {Array.from({ length: 9 }).map((_, i) => (
          <span
            key={i}
            className={`pip-dot ${activeIndices.includes(i) ? "active" : ""}`}
          />
        ))}
      </div>
    );
  }

  // 10 to 12 use a 3x4 grid
  const activeIndices12 = {
    10: [0, 1, 2, 3, 4, 7, 8, 9, 10, 11],
    11: [0, 1, 2, 3, 4, 5, 7, 8, 9, 10, 11],
    12: [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11],
  }[count] || [];

  return (
    <div className="pip-grid pip-grid-3x4">
      {Array.from({ length: 12 }).map((_, i) => (
        <span
          key={i}
          className={`pip-dot ${activeIndices12.includes(i) ? "active" : ""}`}
        />
      ))}
    </div>
  );
}

function DominoTile({
  left = 0,
  right = 0,
  orientation = "vertical", // "vertical" | "horizontal"
  selected = false,
  disabled = false,
  isPlayable = false,
  onClick,
  size = "medium", // "small" | "medium" | "large"
  className = "",
}) {
  const handleClick = (e) => {
    if (disabled || !onClick) return;
    onClick(e);
  };

  return (
    <div
      className={`domino-tile ${orientation} size-${size} ${selected ? "selected" : ""} ${
        isPlayable ? "playable" : ""
      } ${disabled ? "disabled" : ""} ${className}`}
      onClick={handleClick}
      role={onClick && !disabled ? "button" : undefined}
      tabIndex={onClick && !disabled ? 0 : undefined}
      title={`Domino [${left}|${right}]`}
    >
      <div className="tile-half half-first">{renderPips(left)}</div>
      <div className="tile-divider" />
      <div className="tile-half half-second">{renderPips(right)}</div>
    </div>
  );
}

export default DominoTile;

