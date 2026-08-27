import React from "react";

class ErrorBoundary extends React.Component {
  constructor(props) {
    super(props);
    this.state = { hasError: false, error: null, errorInfo: null };
  }

  static getDerivedStateFromError(error) {
    return { hasError: true, error };
  }

  componentDidCatch(error, errorInfo) {
    console.error("ErrorBoundary caught an error:", error, errorInfo);
    this.setState({ error, errorInfo });
  }

  handleReset = () => {
    this.setState({ hasError: false, error: null, errorInfo: null });
    window.location.href = "/";
  };

  render() {
    if (this.state.hasError) {
      return (
        <div style={{
          minHeight: "100vh",
          backgroundColor: "#0e1117",
          color: "#f3f4f6",
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          justifyContent: "center",
          padding: "24px",
          fontFamily: "system-ui, sans-serif"
        }}>
          <div style={{
            background: "#161922",
            border: "1px solid #2e3344",
            borderRadius: "16px",
            padding: "32px",
            maxWidth: "500px",
            width: "100%",
            textAlign: "center",
            boxShadow: "0 10px 30px rgba(0,0,0,0.5)"
          }}>
            <h2 style={{ color: "#ef4444", margin: "0 0 12px" }}>Game Encountered an Error!</h2>
            <p style={{ color: "#9ca3af", fontSize: "14px", marginBottom: "16px" }}>
              {this.state.error?.message || "An unexpected error occurred during rendering."}
            </p>
            <button
              onClick={this.handleReset}
              style={{
                backgroundColor: "#22c55e",
                color: "#052e16",
                border: "none",
                padding: "10px 24px",
                borderRadius: "8px",
                fontWeight: "700",
                fontSize: "15px",
                cursor: "pointer"
              }}
            >
              Return to Home
            </button>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}

export default ErrorBoundary;

