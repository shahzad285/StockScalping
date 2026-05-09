import { FormEvent, useEffect, useMemo, useState } from "react";
import { clearToken, getToken, setToken } from "./auth/authStorage";
import { AccountProfile, getProfile, login, LoginMethod, requestLoginOtp, smartApiLogin } from "./api/accountApi";
import { getHoldings, getPrices, HoldingStock, StockPrice } from "./api/stockApi";
import { getOrders, OrderDetails } from "./api/orderApi";

type View = "holdings" | "prices" | "orders";

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
    if (isLoggedIn) {
      void loadDashboard();
    }
  }, [isLoggedIn]);

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
          <button type="button" onClick={loadDashboard} disabled={isBusy}>
            {isBusy ? "Refreshing..." : "Refresh"}
          </button>
          <button type="button" className="secondary" onClick={handleLogout}>
            Logout
          </button>
        </div>
      </header>

      {message && <p className={messageType === "success" ? "success-text" : "error-text"}>{message}</p>}

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
    </main>
  );
}

export default App;
