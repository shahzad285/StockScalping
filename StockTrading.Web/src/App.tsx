import { FormEvent, useEffect, useMemo, useState } from "react";
import { clearToken, getToken, setToken } from "./auth/authStorage";
import { AccountProfile, getProfile, login, LoginMethod, requestLoginOtp, smartApiLogin } from "./api/accountApi";
import { getHoldings, getPrices, HoldingStock, StockPrice } from "./api/stockApi";
import { getOrders, OrderDetails } from "./api/orderApi";
import {
  createWatchlist,
  deleteStockFromWatchlist,
  deleteWatchlist,
  getWatchlists,
  getWatchlistStocks,
  saveStockToWatchlist,
  Watchlist,
  WatchlistStock
} from "./api/watchlistApi";

type View = "holdings" | "prices" | "orders";
type Page = "dashboard" | "watchlists";

const emptyWatchlistStock: WatchlistStock = {
  symbol: "",
  exchange: "NSE",
  symbolToken: "",
  tradingSymbol: "",
  purchaseRate: null,
  salesRate: null
};

function formatMoney(value: number): string {
  return new Intl.NumberFormat("en-IN", {
    style: "currency",
    currency: "INR",
    maximumFractionDigits: 2
  }).format(value);
}

function App() {
  const [token, setCurrentToken] = useState(() => getToken());
  const [loginMethod, setLoginMethod] = useState<LoginMethod>(LoginMethod.EmailOtp);
  const [email, setEmail] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [otp, setOtp] = useState("");
  const [otpExpiresAtUtc, setOtpExpiresAtUtc] = useState<string | null>(null);
  const [isOtpRequested, setIsOtpRequested] = useState(false);
  const [smartApiTotp, setSmartApiTotp] = useState("");
  const [isBrokerConnecting, setIsBrokerConnecting] = useState(false);
  const [isBrokerConnected, setIsBrokerConnected] = useState(false);
  const [profile, setProfile] = useState<AccountProfile | null>(null);
  const [holdings, setHoldings] = useState<HoldingStock[]>([]);
  const [prices, setPrices] = useState<StockPrice[]>([]);
  const [orders, setOrders] = useState<OrderDetails[]>([]);
  const [page, setPage] = useState<Page>("dashboard");
  const [watchlists, setWatchlists] = useState<Watchlist[]>([]);
  const [selectedWatchlistId, setSelectedWatchlistId] = useState<number | null>(null);
  const [selectedWatchlistStocks, setSelectedWatchlistStocks] = useState<WatchlistStock[]>([]);
  const [newWatchlistName, setNewWatchlistName] = useState("");
  const [selectedWatchlistForm, setSelectedWatchlistForm] = useState<WatchlistStock>(emptyWatchlistStock);
  const [totalProfitLoss, setTotalProfitLoss] = useState(0);
  const [activeView, setActiveView] = useState<View>("holdings");
  const [isBusy, setIsBusy] = useState(false);
  const [message, setMessage] = useState("");
  const [messageType, setMessageType] = useState<"error" | "success">("error");

  const isLoggedIn = Boolean(token);
  const positiveTotal = totalProfitLoss >= 0;

  const summary = useMemo(
    () => ({
      holdings: holdings.length,
      prices: prices.filter((price) => price.isFetched).length,
      orders: orders.length
    }),
    [holdings, orders, prices]
  );

  async function loadDashboard() {
    setIsBusy(true);
    setMessage("");

    try {
      const profileResult = await getProfile();
      setProfile(profileResult.profile);
      setIsBrokerConnected(true);
    } catch (error) {
      setProfile(null);
      setIsBrokerConnected(false);
      setMessageType("error");
      setMessage(error instanceof Error ? error.message : "Broker session unavailable.");
    }

    try {
      const [holdingsResult, pricesResult, ordersResult] = await Promise.all([
        getHoldings(),
        getPrices(),
        getOrders()
      ]);

      setHoldings(holdingsResult.stocks);
      setTotalProfitLoss(holdingsResult.totalProfitLoss);
      setPrices(pricesResult.prices);
      setOrders(ordersResult.orders);
    } catch (error) {
      setMessageType("error");
      setMessage(error instanceof Error ? error.message : "Unable to load dashboard.");
    } finally {
      setIsBusy(false);
    }
  }

  async function handleSmartApiLogin(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsBrokerConnecting(true);
    setMessage("");

    try {
      const result = await smartApiLogin({ totp: smartApiTotp.trim() || undefined });
      setSmartApiTotp("");
      setIsBrokerConnected(true);
      setMessageType("success");
      setMessage(result.message);
      await loadDashboard();
    } catch (error) {
      setIsBrokerConnected(false);
      setMessageType("error");
      setMessage(error instanceof Error ? error.message : "Broker login failed.");
    } finally {
      setIsBrokerConnecting(false);
    }
  }

  async function loadWatchlists(nextSelectedId?: number) {
    setIsBusy(true);
    setMessage("");

    try {
      const result = await getWatchlists();
      setWatchlists(result.watchlists);

      const selectedId = nextSelectedId ?? selectedWatchlistId ?? result.watchlists[0]?.id ?? null;
      setSelectedWatchlistId(selectedId);

      if (selectedId) {
        const stocksResult = await getWatchlistStocks(selectedId);
        setSelectedWatchlistStocks(stocksResult.stocks);
      } else {
        setSelectedWatchlistStocks([]);
      }
    } catch (error) {
      setMessageType("error");
      setMessage(error instanceof Error ? error.message : "Unable to load watchlists.");
    } finally {
      setIsBusy(false);
    }
  }

  async function handleCreateWatchlist(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsBusy(true);
    setMessage("");

    try {
      const result = await createWatchlist(newWatchlistName.trim());
      setNewWatchlistName("");
      setMessageType("success");
      setMessage("Watchlist created.");
      await loadWatchlists(result.watchlist.id);
    } catch (error) {
      setMessageType("error");
      setMessage(error instanceof Error ? error.message : "Unable to create watchlist.");
    } finally {
      setIsBusy(false);
    }
  }

  async function handleSelectWatchlist(id: number) {
    setSelectedWatchlistId(id);
    setIsBusy(true);
    setMessage("");

    try {
      const result = await getWatchlistStocks(id);
      setSelectedWatchlistStocks(result.stocks);
    } catch (error) {
      setMessageType("error");
      setMessage(error instanceof Error ? error.message : "Unable to load watchlist stocks.");
    } finally {
      setIsBusy(false);
    }
  }

  async function handleDeleteWatchlist(id: number) {
    setIsBusy(true);
    setMessage("");

    try {
      await deleteWatchlist(id);
      setMessageType("success");
      setMessage("Watchlist removed.");
      await loadWatchlists(selectedWatchlistId === id ? undefined : selectedWatchlistId ?? undefined);
    } catch (error) {
      setMessageType("error");
      setMessage(error instanceof Error ? error.message : "Unable to remove watchlist.");
    } finally {
      setIsBusy(false);
    }
  }

  async function handleSelectedWatchlistSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!selectedWatchlistId) {
      setMessageType("error");
      setMessage("Create or select a watchlist first.");
      return;
    }

    setIsBusy(true);
    setMessage("");

    try {
      await saveStockToWatchlist(selectedWatchlistId, {
        ...selectedWatchlistForm,
        purchaseRate: selectedWatchlistForm.purchaseRate ?? null,
        salesRate: selectedWatchlistForm.salesRate ?? null
      });
      const result = await getWatchlistStocks(selectedWatchlistId);
      setSelectedWatchlistStocks(result.stocks);
      setSelectedWatchlistForm(emptyWatchlistStock);
      setMessageType("success");
      setMessage("Watchlist stock saved.");
    } catch (error) {
      setMessageType("error");
      setMessage(error instanceof Error ? error.message : "Unable to save watchlist stock.");
    } finally {
      setIsBusy(false);
    }
  }

  async function handleDeleteSelectedWatchlistStock(stock: WatchlistStock) {
    if (!selectedWatchlistId || !stock.watchlistItemId) {
      return;
    }

    setIsBusy(true);
    setMessage("");

    try {
      await deleteStockFromWatchlist(selectedWatchlistId, stock.watchlistItemId);
      setSelectedWatchlistStocks((current) => current.filter((item) => item.watchlistItemId !== stock.watchlistItemId));
      setMessageType("success");
      setMessage("Watchlist stock removed.");
    } catch (error) {
      setMessageType("error");
      setMessage(error instanceof Error ? error.message : "Unable to remove watchlist stock.");
    } finally {
      setIsBusy(false);
    }
  }

  const isEmailLogin = loginMethod === LoginMethod.EmailOtp;
  const isPhoneLogin = loginMethod === LoginMethod.PhoneOtp;
  const isGoogleLogin = loginMethod === LoginMethod.GoogleOAuth;
  const loginIdentifier = isEmailLogin ? email.trim() : phoneNumber.trim();
  const canRequestOtp = Boolean(loginIdentifier);
  const canLogin = canRequestOtp && otp.trim().length > 0;

  async function handleRequestOtp(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsBusy(true);
    setMessage("");

    try {
      const result = await requestLoginOtp({
        loginMethod,
        email: isEmailLogin ? email.trim() : undefined,
        phoneNumber: isEmailLogin ? undefined : phoneNumber.trim()
      });
      setOtp("");
      setOtpExpiresAtUtc(result.expiresAtUtc);
      setIsOtpRequested(true);
      setMessageType("success");
      setMessage(result.otp ? `OTP sent. Dev OTP: ${result.otp}` : result.message);
    } catch (error) {
      setMessageType("error");
      setMessage(error instanceof Error ? error.message : "Unable to send OTP.");
    } finally {
      setIsBusy(false);
    }
  }

  async function handleLogin(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsBusy(true);
    setMessage("");

    try {
      const result = await login({
        loginMethod,
        email: isEmailLogin ? email.trim() : undefined,
        phoneNumber: isEmailLogin ? undefined : phoneNumber.trim(),
        otp: otp.trim()
      });
      setToken(result.token);
      setCurrentToken(result.token);
      setOtp("");
      setOtpExpiresAtUtc(null);
      setIsOtpRequested(false);
    } catch (error) {
      setMessageType("error");
      setMessage(error instanceof Error ? error.message : "Login failed.");
    } finally {
      setIsBusy(false);
    }
  }

  function handleLogout() {
    clearToken();
    setCurrentToken(null);
    setProfile(null);
    setIsBrokerConnected(false);
    setHoldings([]);
    setPrices([]);
    setOrders([]);
    setTotalProfitLoss(0);
  }

  function handleLoginMethodChange(method: LoginMethod) {
    setLoginMethod(method);
    setOtp("");
    setOtpExpiresAtUtc(null);
    setIsOtpRequested(false);
    setMessage("");
    setMessageType("error");
  }

  useEffect(() => {
    if (token) {
      void loadDashboard();
    }
  }, [token]);

  useEffect(() => {
    if (token && page === "watchlists") {
      void loadWatchlists();
    }
  }, [token, page]);

  if (!isLoggedIn) {
    return (
      <main className="login-screen">
        <section className="login-panel">
          <div>
            <p className="eyebrow">StockTrading</p>
            <h1>Login</h1>
          </div>

          <div className="login-methods" role="tablist" aria-label="Login method">
            <button
              type="button"
              className={isEmailLogin ? "active" : ""}
              aria-selected={isEmailLogin}
              onClick={() => handleLoginMethodChange(LoginMethod.EmailOtp)}
            >
              Email
            </button>
            <button
              type="button"
              className={isPhoneLogin ? "active" : ""}
              aria-selected={isPhoneLogin}
              onClick={() => handleLoginMethodChange(LoginMethod.PhoneOtp)}
            >
              Mobile
            </button>
            <button
              type="button"
              className={isGoogleLogin ? "active" : ""}
              aria-selected={isGoogleLogin}
              onClick={() => handleLoginMethodChange(LoginMethod.GoogleOAuth)}
            >
              Google
            </button>
          </div>

          {isGoogleLogin ? (
            <div className="oauth-panel">
              <button type="button" disabled>
                Continue with Google
              </button>
              <p className="helper-text">Google OAuth is configured as a login method, but the server flow is not wired yet.</p>
            </div>
          ) : (
            <>
              <form onSubmit={handleRequestOtp} className="login-form">
                <label htmlFor={isEmailLogin ? "email" : "phoneNumber"}>{isEmailLogin ? "Email" : "Mobile number"}</label>
                <input
                  id={isEmailLogin ? "email" : "phoneNumber"}
                  type={isEmailLogin ? "email" : "tel"}
                  inputMode={isEmailLogin ? "email" : "tel"}
                  value={isEmailLogin ? email : phoneNumber}
                  onChange={(event) => (isEmailLogin ? setEmail(event.target.value) : setPhoneNumber(event.target.value))}
                  placeholder={isEmailLogin ? "you@example.com" : "+919876543210"}
                  required
                />
                <button type="submit" disabled={isBusy || !canRequestOtp}>
                  {isBusy ? "Sending..." : isOtpRequested ? "Resend OTP" : "Send OTP"}
                </button>
              </form>

              <form onSubmit={handleLogin} className="login-form">
                <label htmlFor="otp">OTP</label>
                <input
                  id="otp"
                  inputMode="numeric"
                  maxLength={6}
                  value={otp}
                  onChange={(event) => setOtp(event.target.value)}
                  placeholder="Enter 6 digit OTP"
                  required
                />
                <button type="submit" disabled={isBusy || !canLogin}>
                  {isBusy ? "Signing in..." : "Sign in"}
                </button>
              </form>

              {otpExpiresAtUtc && (
                <p className="helper-text">
                  OTP expires at {new Date(otpExpiresAtUtc).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}.
                </p>
              )}
            </>
          )}

          {message && <p className={messageType === "success" ? "success-text" : "error-text"}>{message}</p>}
        </section>
      </main>
    );
  }

  return (
    <main className="app-shell">
      <header className="topbar">
        <div>
          <p className="eyebrow">StockTrading</p>
          <h1>{profile?.name || "Dashboard"}</h1>
        </div>
        <div className="topbar-actions">
          <button type="button" className={page === "dashboard" ? "" : "secondary"} onClick={() => setPage("dashboard")}>
            Dashboard
          </button>
          <button type="button" className={page === "watchlists" ? "" : "secondary"} onClick={() => setPage("watchlists")}>
            Watchlists
          </button>
          <button type="button" onClick={loadDashboard} disabled={isBusy}>
            {isBusy ? "Refreshing..." : "Refresh"}
          </button>
          <button type="button" className="secondary" onClick={handleLogout}>
            Logout
          </button>
        </div>
      </header>

      {message && <p className={messageType === "success" ? "success-text" : "error-text"}>{message}</p>}

      {page === "watchlists" && (
        <section className="watchlists-page">
          <div className="watchlists-sidebar">
            <div className="section-heading">
              <div>
                <p className="eyebrow">Watchlists</p>
                <h2>Lists</h2>
              </div>
            </div>

            <form className="watchlist-create-form" onSubmit={handleCreateWatchlist}>
              <input
                value={newWatchlistName}
                onChange={(event) => setNewWatchlistName(event.target.value)}
                placeholder="New watchlist"
                required
              />
              <button type="submit" disabled={isBusy}>
                Create
              </button>
            </form>

            <div className="watchlist-list">
              {watchlists.map((watchlist) => (
                <article className={selectedWatchlistId === watchlist.id ? "active" : ""} key={watchlist.id}>
                  <button type="button" onClick={() => void handleSelectWatchlist(watchlist.id)}>
                    {watchlist.name}
                  </button>
                  <button
                    type="button"
                    className="secondary"
                    onClick={() => void handleDeleteWatchlist(watchlist.id)}
                    disabled={isBusy}
                  >
                    Remove
                  </button>
                </article>
              ))}
            </div>
          </div>

          <div className="watchlist-detail">
            <div className="section-heading">
              <div>
                <p className="eyebrow">Stocks</p>
                <h2>{watchlists.find((watchlist) => watchlist.id === selectedWatchlistId)?.name || "Select a watchlist"}</h2>
              </div>
            </div>

            <form className="stock-plan-form" onSubmit={handleSelectedWatchlistSubmit}>
              <label>
                Symbol
                <input
                  value={selectedWatchlistForm.symbol}
                  onChange={(event) => setSelectedWatchlistForm((current) => ({ ...current, symbol: event.target.value }))}
                  placeholder="RELIANCE"
                  required
                />
              </label>
              <label>
                Exchange
                <input
                  value={selectedWatchlistForm.exchange}
                  onChange={(event) => setSelectedWatchlistForm((current) => ({ ...current, exchange: event.target.value }))}
                  placeholder="NSE"
                  required
                />
              </label>
              <label>
                Symbol token
                <input
                  value={selectedWatchlistForm.symbolToken}
                  onChange={(event) => setSelectedWatchlistForm((current) => ({ ...current, symbolToken: event.target.value }))}
                  placeholder="2885"
                  required
                />
              </label>
              <label>
                Trading symbol
                <input
                  value={selectedWatchlistForm.tradingSymbol}
                  onChange={(event) => setSelectedWatchlistForm((current) => ({ ...current, tradingSymbol: event.target.value }))}
                  placeholder="RELIANCE-EQ"
                />
              </label>
              <label>
                Buy at
                <input
                  type="number"
                  step="0.05"
                  value={selectedWatchlistForm.purchaseRate ?? ""}
                  onChange={(event) =>
                    setSelectedWatchlistForm((current) => ({
                      ...current,
                      purchaseRate: event.target.value ? Number(event.target.value) : null
                    }))
                  }
                  placeholder="0.00"
                />
              </label>
              <label>
                Sell at
                <input
                  type="number"
                  step="0.05"
                  value={selectedWatchlistForm.salesRate ?? ""}
                  onChange={(event) =>
                    setSelectedWatchlistForm((current) => ({
                      ...current,
                      salesRate: event.target.value ? Number(event.target.value) : null
                    }))
                  }
                  placeholder="0.00"
                />
              </label>
              <button type="submit" disabled={isBusy || !selectedWatchlistId}>
                Save stock
              </button>
            </form>

            <div className="planned-stocks">
              {selectedWatchlistStocks.map((stock) => (
                <article key={stock.watchlistItemId || `${stock.exchange}-${stock.symbolToken}`}>
                  <div>
                    <strong>{stock.tradingSymbol || stock.symbol}</strong>
                    <span>
                      {stock.exchange} · {stock.symbolToken}
                    </span>
                  </div>
                  <div>
                    <span>Buy</span>
                    <strong>{stock.purchaseRate ? formatMoney(stock.purchaseRate) : "-"}</strong>
                  </div>
                  <div>
                    <span>Sell</span>
                    <strong>{stock.salesRate ? formatMoney(stock.salesRate) : "-"}</strong>
                  </div>
                  <button type="button" className="secondary" onClick={() => void handleDeleteSelectedWatchlistStock(stock)} disabled={isBusy}>
                    Remove
                  </button>
                </article>
              ))}
            </div>
          </div>
        </section>
      )}

      {page === "dashboard" && (
        <>

      <section className="broker-panel">
        <div className="broker-status">
          <div>
            <span>Broker</span>
            <strong>{profile?.broker || "Angel One"}</strong>
          </div>
          <div>
            <span>Session</span>
            <strong className={isBrokerConnected ? "positive" : "negative"}>
              {isBrokerConnected ? "Connected" : "Not connected"}
            </strong>
          </div>
          <div>
            <span>Scope</span>
            <strong>Global</strong>
          </div>
        </div>

        <form className="broker-login" onSubmit={handleSmartApiLogin}>
          <label htmlFor="smartApiTotp">SmartAPI TOTP</label>
          <input
            id="smartApiTotp"
            inputMode="numeric"
            maxLength={6}
            value={smartApiTotp}
            onChange={(event) => setSmartApiTotp(event.target.value)}
            placeholder="Enter TOTP"
          />
          <button type="submit" disabled={isBrokerConnecting || isBusy}>
            {isBrokerConnecting ? "Connecting..." : isBrokerConnected ? "Refresh session" : "Connect broker"}
          </button>
        </form>
      </section>


      <section className="summary-grid">
        <article>
          <span>Total P/L</span>
          <strong className={positiveTotal ? "positive" : "negative"}>{formatMoney(totalProfitLoss)}</strong>
        </article>
        <article>
          <span>Holdings</span>
          <strong>{summary.holdings}</strong>
        </article>
        <article>
          <span>Live Prices</span>
          <strong>{summary.prices}</strong>
        </article>
        <article>
          <span>Orders</span>
          <strong>{summary.orders}</strong>
        </article>
      </section>

      <nav className="tabs" aria-label="Dashboard views">
        <button className={activeView === "holdings" ? "active" : ""} onClick={() => setActiveView("holdings")}>
          Holdings
        </button>
        <button className={activeView === "prices" ? "active" : ""} onClick={() => setActiveView("prices")}>
          Prices
        </button>
        <button className={activeView === "orders" ? "active" : ""} onClick={() => setActiveView("orders")}>
          Orders
        </button>
      </nav>

      {activeView === "holdings" && (
        <section className="table-section">
          <table>
            <thead>
              <tr>
                <th>Symbol</th>
                <th>Qty</th>
                <th>Avg</th>
                <th>LTP</th>
                <th>P/L</th>
              </tr>
            </thead>
            <tbody>
              {holdings.map((holding) => (
                <tr key={`${holding.exchange}-${holding.symbolToken || holding.tradingSymbol}`}>
                  <td>{holding.tradingSymbol || holding.stockName}</td>
                  <td>{holding.totalStocks}</td>
                  <td>{formatMoney(holding.purchasePrice)}</td>
                  <td>{formatMoney(holding.currentPrice)}</td>
                  <td className={holding.totalGainOrLoss >= 0 ? "positive" : "negative"}>
                    {formatMoney(holding.totalGainOrLoss)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      )}

      {activeView === "prices" && (
        <section className="table-section">
          <table>
            <thead>
              <tr>
                <th>Symbol</th>
                <th>Exchange</th>
                <th>LTP</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {prices.map((price) => (
                <tr key={`${price.exchange}-${price.symbolToken || price.tradingSymbol || price.symbol}`}>
                  <td>{price.tradingSymbol || price.symbol}</td>
                  <td>{price.exchange}</td>
                  <td>{formatMoney(price.lastTradedPrice)}</td>
                  <td>{price.isFetched ? "Fetched" : price.message}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      )}

      {activeView === "orders" && (
        <section className="table-section">
          <table>
            <thead>
              <tr>
                <th>Order</th>
                <th>Symbol</th>
                <th>Type</th>
                <th>Qty</th>
                <th>Avg</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {orders.map((order) => (
                <tr key={order.orderId}>
                  <td>{order.orderId}</td>
                  <td>{order.tradingSymbol}</td>
                  <td>{order.transactionType}</td>
                  <td>{order.filledShares || order.quantity}</td>
                  <td>{formatMoney(order.averagePrice)}</td>
                  <td>{order.statusCategory || order.status}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      )}
        </>
      )}
    </main>
  );
}

export default App;
