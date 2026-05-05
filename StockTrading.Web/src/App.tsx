import { FormEvent, useEffect, useMemo, useState } from "react";
import { clearToken, getToken, setToken } from "./auth/authStorage";
import { AccountProfile, getProfile, login } from "./api/accountApi";
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
  const [totp, setTotp] = useState("");
  const [profile, setProfile] = useState<AccountProfile | null>(null);
  const [holdings, setHoldings] = useState<HoldingStock[]>([]);
  const [prices, setPrices] = useState<StockPrice[]>([]);
  const [orders, setOrders] = useState<OrderDetails[]>([]);
  const [totalProfitLoss, setTotalProfitLoss] = useState(0);
  const [activeView, setActiveView] = useState<View>("holdings");
  const [isBusy, setIsBusy] = useState(false);
  const [message, setMessage] = useState("");

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
      const [profileResult, holdingsResult, pricesResult, ordersResult] = await Promise.all([
        getProfile(),
        getHoldings(),
        getPrices(),
        getOrders()
      ]);

      setProfile(profileResult.profile);
      setHoldings(holdingsResult.stocks);
      setTotalProfitLoss(holdingsResult.totalProfitLoss);
      setPrices(pricesResult.prices);
      setOrders(ordersResult.orders);
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Unable to load dashboard.");
    } finally {
      setIsBusy(false);
    }
  }

  async function handleLogin(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsBusy(true);
    setMessage("");

    try {
      const result = await login(totp);
      setToken(result.token);
      setCurrentToken(result.token);
      setTotp("");
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Login failed.");
    } finally {
      setIsBusy(false);
    }
  }

  function handleLogout() {
    clearToken();
    setCurrentToken(null);
    setProfile(null);
    setHoldings([]);
    setPrices([]);
    setOrders([]);
    setTotalProfitLoss(0);
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
            <h1>Broker Login</h1>
          </div>

          <form onSubmit={handleLogin} className="login-form">
            <label htmlFor="totp">TOTP</label>
            <input
              id="totp"
              inputMode="numeric"
              value={totp}
              onChange={(event) => setTotp(event.target.value)}
              placeholder="Enter 6 digit code"
              required
            />
            <button type="submit" disabled={isBusy}>
              {isBusy ? "Signing in..." : "Sign in"}
            </button>
          </form>

          {message && <p className="error-text">{message}</p>}
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

      {message && <p className="error-text">{message}</p>}

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
