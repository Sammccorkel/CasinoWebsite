window.auth = {
  login: async (username, password) => {
    const response = await fetch("/api/auth/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ username, password }),
      credentials: "include"
    });

    const text = await response.text();
    return { ok: response.ok, status: response.status, text };
  },
  logout: async () => {
    const response = await fetch("/api/auth/logout", {
      method: "POST",
      credentials: "include"
    });

    const text = await response.text();
    return { ok: response.ok, status: response.status, text };
  },
  currentUser: async () => {
    const response = await fetch("/api/auth/current-user", {
      method: "GET",
      credentials: "include"
    });

    const text = await response.text();
    return { ok: response.ok, status: response.status, text };
  }
};
