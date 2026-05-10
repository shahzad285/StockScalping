import { FormEvent, MouseEvent, useCallback, useEffect, useMemo, useState } from "react";
import { clearToken, getToken, setToken } from "./auth/authStorage";
import { AccountProfile, getProfile, login, LoginMethod, requestLoginOtp, smartApiLogin } from "./api/accountApi";
import { authExpiredEventName } from "./api/apiClient";
import {
  getHoldings,
  getPrices,
  getStockChart,
  HoldingStock,
  StockCandle,
  StockChartRange,
  StockExchange,
  StockPrice,
  StockSearchResult
} from "./api/stockApi";
import { getOrders, OrderDetails } from "./api/orderApi";
import { deleteTradePlan, getTradePlans, saveTradePlan, searchTradePlanStocks, TradePlan } from "./api/tradePlanApi";
import {
  deleteWatchlistStockById,
  getWatchlistStocks,
  searchWatchlistStocks,
  saveWatchlistStock,
  WatchlistStock
} from "./api/watchlistApi";

type View = "holdings" | "prices" | "orders";
type Page = "dashboard" | "watchlists" | "tradeplans";

const chartRanges: { label: string; value: StockChartRange }[] = [
  { label: "1D", value: "OneDay" },
  { label: "1W", value: "OneWeek" },
  { label: "1M", value: "OneMonth" },
  { label: "6M", value: "SixMonths" },
  { label: "1Y", value: "OneYear" }
];

const emptyWatchlistStock: WatchlistStock = {
  symbol: "",
  exchange: "NSE",
  symbolToken: "",
  tradingSymbol: "",
  purchaseRate: null,
  salesRate: null,
  assetType: "Unknown",
  theme: "",
  sector: "",
  industry: "",
  classificationReason: "",
  confidenceScore: null
};

const stockAssetTypes = [
  "Unknown",
  "SlowGrower",
  "Stalwart",
  "FastGrower",
  "Cyclical",
  "Turnaround",
  "AssetPlay"
];

const emptyTradePlan: TradePlan = {
  buyPrice: 0,
  sellPrice: 0,
  quantity: 1,
  maxBudget: null,
  status: "Active",
  isActive: true,
  repeatEnabled: true,
  symbol: "",
  exchange: "NSE",
  symbolToken: "",
  tradingSymbol: ""
};

function formatMoney(value: number): string {
  return new Intl.NumberFormat("en-IN", {
    style: "currency",
    currency: "INR",
    maximumFractionDigits: 2
  }).format(value);
}

function StockLineChart({ candles }: { candles: StockCandle[] }) {
  const [hoverIndex, setHoverIndex] = useState<number | null>(null);
  const width = 1000;
  const height = 420;
  const paddingLeft = 84;
  const paddingRight = 40;
  const paddingTop = 46;
  const paddingBottom = 58;
  const closes = candles.map((candle) => candle.close);
  const min = Math.min(...closes);
  const max = Math.max(...closes);
  const range = max - min || 1;
  const chartPoints = candles.map((candle, index) => {
    const x = paddingLeft + (index / Math.max(candles.length - 1, 1)) * (width - paddingLeft - paddingRight);
    const y = height - paddingBottom - ((candle.close - min) / range) * (height - paddingTop - paddingBottom);
    return { candle, x, y };
  });
  const points = chartPoints.map((point) => `${point.x.toFixed(2)},${point.y.toFixed(2)}`).join(" ");
  const first = candles[0];
  const last = candles[candles.length - 1];
  const positive = last.close >= first.close;
  const hoverPoint = hoverIndex == null ? null : chartPoints[hoverIndex];
  const yTicks = Array.from({ length: 5 }, (_, index) => {
    const value = min + (range * (4 - index)) / 4;
    const y = paddingTop + (index / 4) * (height - paddingTop - paddingBottom);
    return { value, y };
  });
  const xTickCount = Math.min(5, candles.length);
  const xTicks = Array.from({ length: xTickCount }, (_, index) => {
    const candleIndex = Math.round((index / Math.max(xTickCount - 1, 1)) * (candles.length - 1));
    const point = chartPoints[candleIndex];
    return {
      x: point.x,
      label: new Date(point.candle.time).toLocaleDateString([], { day: "2-digit", month: "short" })
    };
  });

  function handleChartHover(event: MouseEvent<SVGSVGElement>) {
    const bounds = event.currentTarget.getBoundingClientRect();
    const relativeX = ((event.clientX - bounds.left) / bounds.width) * width;
    const index = Math.round(
      ((relativeX - paddingLeft) / (width - paddingLeft - paddingRight)) * Math.max(candles.length - 1, 1)
    );
    setHoverIndex(Math.min(Math.max(index, 0), candles.length - 1));
  }

  return (
    <svg
      className="stock-chart"
      viewBox={`0 0 ${width} ${height}`}
      role="img"
      aria-label="Stock closing price chart"
      onMouseMove={handleChartHover}
      onMouseLeave={() => setHoverIndex(null)}
    >
      {yTicks.map((tick) => (
        <g className="chart-grid" key={tick.y}>
          <line x1={paddingLeft} y1={tick.y} x2={width - paddingRight} y2={tick.y} />
          <text className="chart-y-label" x={paddingLeft - 10} y={tick.y + 4}>
            {tick.value.toFixed(2)}
          </text>
        </g>
      ))}
      {xTicks.map((tick) => (
        <g className="chart-grid" key={`${tick.x}-${tick.label}`}>
          <line x1={tick.x} y1={paddingTop} x2={tick.x} y2={height - paddingBottom} />
          <text className="chart-x-label" x={tick.x} y={height - 18}>
            {tick.label}
          </text>
        </g>
      ))}
      <line className="chart-axis" x1={paddingLeft} y1={paddingTop} x2={paddingLeft} y2={height - paddingBottom} />
      <line className="chart-axis" x1={paddingLeft} y1={height - paddingBottom} x2={width - paddingRight} y2={height - paddingBottom} />
      <text className="chart-bound-label chart-bound-max" x={width - paddingRight} y={20}>
        Max {formatMoney(max)}
      </text>
      <text className="chart-bound-label chart-bound-max" x={width - paddingRight} y={40}>
        Min {formatMoney(min)}
      </text>
      <polyline className={positive ? "positive-line" : "negative-line"} points={points} />
      {hoverPoint && (
        <g className="chart-hover">
          <line x1={hoverPoint.x} y1={paddingTop} x2={hoverPoint.x} y2={height - paddingBottom} />
          <circle cx={hoverPoint.x} cy={hoverPoint.y} r="5" />
          <rect
            x={Math.min(hoverPoint.x + 12, width - 190)}
            y={Math.max(hoverPoint.y - 48, paddingTop)}
            width="178"
            height="42"
            rx="6"
          />
          <text x={Math.min(hoverPoint.x + 22, width - 180)} y={Math.max(hoverPoint.y - 29, paddingTop + 19)}>
            {new Date(hoverPoint.candle.time).toLocaleDateString()}
          </text>
          <text x={Math.min(hoverPoint.x + 22, width - 180)} y={Math.max(hoverPoint.y - 11, paddingTop + 37)}>
            {formatMoney(hoverPoint.candle.close)}
          </text>
        </g>
      )}
    </svg>
  );
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
  const [watchlistStocks, setWatchlistStocks] = useState<WatchlistStock[]>([]);
  const [selectedWatchlistForm, setSelectedWatchlistForm] = useState<WatchlistStock>(emptyWatchlistStock);
  const [watchlistStockSearch, setWatchlistStockSearch] = useState("");
  const [watchlistStockSearchResults, setWatchlistStockSearchResults] = useState<StockSearchResult[]>([]);
  const [isWatchlistStockSearching, setIsWatchlistStockSearching] = useState(false);
  const [chartStock, setChartStock] = useState<WatchlistStock | null>(null);
  const [chartRange, setChartRange] = useState<StockChartRange>("OneMonth");
  const [chartCandles, setChartCandles] = useState<StockCandle[]>([]);
  const [isChartLoading, setIsChartLoading] = useState(false);
  const [tradePlans, setTradePlans] = useState<TradePlan[]>([]);
  const [tradePlanForm, setTradePlanForm] = useState<TradePlan>(emptyTradePlan);
  const [tradePlanStockSearch, setTradePlanStockSearch] = useState("");
  const [tradePlanStockSearchResults, setTradePlanStockSearchResults] = useState<StockSearchResult[]>([]);
  const [isTradePlanStockSearching, setIsTradePlanStockSearching] = useState(false);
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

  async function loadWatchlist() {
    setIsBusy(true);
    setMessage("");

    try {
      const result = await getWatchlistStocks();
      setWatchlistStocks(result.stocks);
    } catch (error) {
      setMessageType("error");
      setMessage(error instanceof Error ? error.message : "Unable to load watchlist.");
    } finally {
      setIsBusy(false);
    }
  }

  async function handleWatchlistStockSearch() {
    const query = watchlistStockSearch.trim() || selectedWatchlistForm.symbol.trim();
    if (!query) {
      setMessageType("error");
      setMessage("Enter a stock to search.");
      return;
    }

    setIsWatchlistStockSearching(true);
    setMessage("");

    try {
      const result = await searchWatchlistStocks(query, selectedWatchlistForm.exchange as StockExchange);
      setWatchlistStockSearchResults(result.stocks);
      if (result.stocks.length === 0) {
        setMessageType("error");
        setMessage("No stocks found.");
      }
    } catch (error) {
      setMessageType("error");
      setMessage(error instanceof Error ? error.message : "Unable to search stocks.");
    } finally {
      setIsWatchlistStockSearching(false);
    }
  }

  function selectWatchlistStock(stock: StockSearchResult) {
    setSelectedWatchlistForm((current) => ({
      ...current,
      symbol: stock.symbol,
      exchange: stock.exchange,
      symbolToken: stock.symbolToken,
      tradingSymbol: stock.tradingSymbol
    }));
    setWatchlistStockSearch(stock.tradingSymbol || stock.symbol);
    setWatchlistStockSearchResults([]);
    setMessage("");
  }

  async function handleSelectedWatchlistSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!selectedWatchlistForm.symbolToken) {
      setMessageType("error");
      setMessage("Search and select a stock first.");
      return;
    }

    setIsBusy(true);
    setMessage("");

    try {
      await saveWatchlistStock({
        ...selectedWatchlistForm,
        purchaseRate: selectedWatchlistForm.purchaseRate ?? null,
        salesRate: selectedWatchlistForm.salesRate ?? null,
        theme: selectedWatchlistForm.theme?.trim() || null,
        sector: selectedWatchlistForm.sector?.trim() || null,
        industry: selectedWatchlistForm.industry?.trim() || null,
        classificationReason: selectedWatchlistForm.classificationReason?.trim() || null,
        confidenceScore: selectedWatchlistForm.confidenceScore ?? null
      });
      const result = await getWatchlistStocks();
      setWatchlistStocks(result.stocks);
      setSelectedWatchlistForm(emptyWatchlistStock);
      setWatchlistStockSearch("");
      setWatchlistStockSearchResults([]);
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
    const watchlistId = stock.watchlistId;
    if (!watchlistId) {
      return;
    }

    setIsBusy(true);
    setMessage("");

    try {
      await deleteWatchlistStockById(watchlistId);
      setWatchlistStocks((current) => current.filter((item) => item.watchlistId !== watchlistId));
      setMessageType("success");
      setMessage("Watchlist stock removed.");
    } catch (error) {
      setMessageType("error");
      setMessage(error instanceof Error ? error.message : "Unable to remove watchlist stock.");
    } finally {
      setIsBusy(false);
    }
  }

  async function loadStockChart(stock: WatchlistStock, range: StockChartRange) {
    if (!stock.symbolToken) {
      setMessageType("error");
      setMessage("Symbol token is required for chart.");
      return;
    }

    setIsChartLoading(true);
    setMessage("");

    try {
      const result = await getStockChart(stock.symbolToken, stock.exchange as StockExchange, range);
      setChartCandles(result.candles);
      if (result.candles.length === 0) {
        setMessageType("error");
        setMessage("No chart data found.");
      }
    } catch (error) {
      setMessageType("error");
      setMessage(error instanceof Error ? error.message : "Unable to load stock chart.");
    } finally {
      setIsChartLoading(false);
    }
  }

  function openStockChart(stock: WatchlistStock) {
    setChartStock(stock);
    setChartRange("OneMonth");
    setChartCandles([]);
    void loadStockChart(stock, "OneMonth");
  }

  function closeStockChart() {
    setChartStock(null);
    setChartCandles([]);
  }

  function handleChartRangeChange(range: StockChartRange) {
    if (!chartStock) {
      return;
    }

    setChartRange(range);
    void loadStockChart(chartStock, range);
  }

  async function loadTradePlans() {
    setIsBusy(true);
    setMessage("");

    try {
      const result = await getTradePlans();
      setTradePlans(result.tradePlans);
    } catch (error) {
      setMessageType("error");
      setMessage(error instanceof Error ? error.message : "Unable to load trade plans.");
    } finally {
      setIsBusy(false);
    }
  }

  async function handleTradePlanStockSearch() {
    const query = tradePlanStockSearch.trim() || tradePlanForm.symbol.trim();
    if (!query) {
      setMessageType("error");
      setMessage("Enter a stock to search.");
      return;
    }

    setIsTradePlanStockSearching(true);
    setMessage("");

    try {
      const result = await searchTradePlanStocks(query, tradePlanForm.exchange as StockExchange);
      setTradePlanStockSearchResults(result.stocks);
      if (result.stocks.length === 0) {
        setMessageType("error");
        setMessage("No stocks found.");
      }
    } catch (error) {
      setMessageType("error");
      setMessage(error instanceof Error ? error.message : "Unable to search stocks.");
    } finally {
      setIsTradePlanStockSearching(false);
    }
  }

  function selectTradePlanStock(stock: StockSearchResult) {
    setTradePlanForm((current) => ({
      ...current,
      symbol: stock.symbol,
      exchange: stock.exchange,
      symbolToken: stock.symbolToken,
      tradingSymbol: stock.tradingSymbol
    }));
    setTradePlanStockSearch(stock.tradingSymbol || stock.symbol);
    setTradePlanStockSearchResults([]);
    setMessage("");
  }

  async function handleTradePlanSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!tradePlanForm.symbolToken) {
      setMessageType("error");
      setMessage("Search and select a stock first.");
      return;
    }

    setIsBusy(true);
    setMessage("");

    try {
      await saveTradePlan({
        ...tradePlanForm,
        maxBudget: tradePlanForm.maxBudget ?? null,
        tradingSymbol: tradePlanForm.tradingSymbol || tradePlanForm.symbol
      });
      setTradePlanForm(emptyTradePlan);
      setTradePlanStockSearch("");
      setTradePlanStockSearchResults([]);
      setMessageType("success");
      setMessage(tradePlanForm.id ? "Trade plan updated." : "Trade plan created.");
      await loadTradePlans();
    } catch (error) {
      setMessageType("error");
      setMessage(error instanceof Error ? error.message : "Unable to save trade plan.");
    } finally {
      setIsBusy(false);
    }
  }

  function handleEditTradePlan(tradePlan: TradePlan) {
    setTradePlanForm({
      ...tradePlan,
      maxBudget: tradePlan.maxBudget ?? null,
      status: tradePlan.status || "Active"
    });
    setTradePlanStockSearch(tradePlan.tradingSymbol || tradePlan.symbol);
    setTradePlanStockSearchResults([]);
    setMessage("");
  }

  async function handleDeleteTradePlan(id: number) {
    setIsBusy(true);
    setMessage("");

    try {
      await deleteTradePlan(id);
      setTradePlans((current) => current.filter((tradePlan) => tradePlan.id !== id));
      if (tradePlanForm.id === id) {
        setTradePlanForm(emptyTradePlan);
      }
      setMessageType("success");
      setMessage("Trade plan removed.");
    } catch (error) {
      setMessageType("error");
      setMessage(error instanceof Error ? error.message : "Unable to remove trade plan.");
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

  const clearSession = useCallback(() => {
    setCurrentToken(null);
    setProfile(null);
    setIsBrokerConnected(false);
    setHoldings([]);
    setPrices([]);
    setOrders([]);
    setTotalProfitLoss(0);
    setWatchlistStocks([]);
    setTradePlans([]);
    setTradePlanForm(emptyTradePlan);
    setPage("dashboard");
  }, []);

  function handleRefresh() {
    if (page === "watchlists") {
      void loadWatchlist();
      return;
    }

    if (page === "tradeplans") {
      void loadTradePlans();
      return;
    }

    void loadDashboard();
  }

  function handleLogout() {
    clearToken();
    clearSession();
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
      void loadWatchlist();
    }
  }, [token, page]);

  useEffect(() => {
    if (token && page === "tradeplans") {
      void loadTradePlans();
    }
  }, [token, page]);

  useEffect(() => {
    function handleAuthExpired() {
      clearSession();
      setMessageType("error");
      setMessage("Your session expired. Please login again.");
    }

    window.addEventListener(authExpiredEventName, handleAuthExpired);

    return () => {
      window.removeEventListener(authExpiredEventName, handleAuthExpired);
    };
  }, [clearSession]);

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
          <button type="button" className={page === "tradeplans" ? "" : "secondary"} onClick={() => setPage("tradeplans")}>
            Trade Plan
          </button>
          <button type="button" onClick={handleRefresh} disabled={isBusy}>
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
          <div className="watchlist-detail">
            <div className="section-heading">
              <div>
                <p className="eyebrow">Smart Watchlist</p>
                <h2>Tracked stocks</h2>
              </div>
            </div>

            <form className="stock-plan-form" onSubmit={handleSelectedWatchlistSubmit}>
              <div className="stock-search-field">
                <label>
                  Search stock
                  <input
                    value={watchlistStockSearch}
                    onChange={(event) => setWatchlistStockSearch(event.target.value)}
                    placeholder="RELIANCE"
                  />
                </label>
                <label>
                  Exchange
                  <select
                    value={selectedWatchlistForm.exchange}
                    onChange={(event) => {
                      setSelectedWatchlistForm((current) => ({
                        ...current,
                        exchange: event.target.value,
                        symbol: "",
                        symbolToken: "",
                        tradingSymbol: ""
                      }));
                      setWatchlistStockSearchResults([]);
                    }}
                  >
                    <option value="NSE">NSE</option>
                    <option value="BSE">BSE</option>
                  </select>
                </label>
                <button type="button" onClick={() => void handleWatchlistStockSearch()} disabled={isWatchlistStockSearching}>
                  {isWatchlistStockSearching ? "Searching..." : "Search"}
                </button>
              </div>

              {watchlistStockSearchResults.length > 0 && (
                <div className="stock-search-results">
                  {watchlistStockSearchResults.map((stock) => (
                    <button type="button" key={`${stock.exchange}-${stock.symbolToken}`} onClick={() => selectWatchlistStock(stock)}>
                      <strong>{stock.tradingSymbol || stock.symbol}</strong>
                      <span>
                        {stock.exchange} - {stock.symbolToken}
                      </span>
                    </button>
                  ))}
                </div>
              )}

              {selectedWatchlistForm.symbolToken && (
                <div className="selected-stock-summary">
                  <div>
                    <span>Symbol</span>
                    <strong>{selectedWatchlistForm.symbol}</strong>
                  </div>
                  <div>
                    <span>Trading symbol</span>
                    <strong>{selectedWatchlistForm.tradingSymbol || "-"}</strong>
                  </div>
                  <div>
                    <span>Exchange</span>
                    <strong>{selectedWatchlistForm.exchange}</strong>
                  </div>
                  <div>
                    <span>Token</span>
                    <strong>{selectedWatchlistForm.symbolToken}</strong>
                  </div>
                </div>
              )}

              <label>
                Asset type
                <select
                  value={selectedWatchlistForm.assetType || "Unknown"}
                  onChange={(event) =>
                    setSelectedWatchlistForm((current) => ({
                      ...current,
                      assetType: event.target.value
                    }))
                  }
                >
                  {stockAssetTypes.map((assetType) => (
                    <option key={assetType} value={assetType}>
                      {assetType}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                Theme
                <input
                  value={selectedWatchlistForm.theme ?? ""}
                  onChange={(event) =>
                    setSelectedWatchlistForm((current) => ({
                      ...current,
                      theme: event.target.value
                    }))
                  }
                  placeholder="Banking, Auto, IT"
                />
              </label>
              <label>
                Sector
                <input
                  value={selectedWatchlistForm.sector ?? ""}
                  onChange={(event) =>
                    setSelectedWatchlistForm((current) => ({
                      ...current,
                      sector: event.target.value
                    }))
                  }
                  placeholder="Optional"
                />
              </label>
              <label>
                Industry
                <input
                  value={selectedWatchlistForm.industry ?? ""}
                  onChange={(event) =>
                    setSelectedWatchlistForm((current) => ({
                      ...current,
                      industry: event.target.value
                    }))
                  }
                  placeholder="Optional"
                />
              </label>
              <label>
                Confidence
                <input
                  type="number"
                  step="0.01"
                  min="0"
                  max="100"
                  value={selectedWatchlistForm.confidenceScore ?? ""}
                  onChange={(event) =>
                    setSelectedWatchlistForm((current) => ({
                      ...current,
                      confidenceScore: event.target.value ? Number(event.target.value) : null
                    }))
                  }
                  placeholder="0-100"
                />
              </label>
              <label>
                Classification reason
                <input
                  value={selectedWatchlistForm.classificationReason ?? ""}
                  onChange={(event) =>
                    setSelectedWatchlistForm((current) => ({
                      ...current,
                      classificationReason: event.target.value
                    }))
                  }
                  placeholder="Why this asset type?"
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
                  placeholder="Optional"
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
                  placeholder="Optional"
                />
              </label>
              <button type="submit" disabled={isBusy}>
                Save stock
              </button>
            </form>

            <div className="planned-stocks">
              {watchlistStocks.map((stock) => (
                <article key={stock.watchlistId || `${stock.exchange}-${stock.symbolToken}`}>
                  <div>
                    <strong>{stock.tradingSymbol || stock.symbol}</strong>
                    <span>
                      {stock.exchange} · {stock.symbolToken}
                    </span>
                  </div>
                  <div>
                    <span>Type</span>
                    <strong>{stock.assetType || "Unknown"}</strong>
                  </div>
                  <div>
                    <span>Theme</span>
                    <strong>{stock.theme || "-"}</strong>
                  </div>
                  <div>
                    <span>Buy</span>
                    <strong>{stock.purchaseRate != null ? formatMoney(stock.purchaseRate) : "-"}</strong>
                  </div>
                  <div>
                    <span>Sell</span>
                    <strong>{stock.salesRate != null ? formatMoney(stock.salesRate) : "-"}</strong>
                  </div>
                  <div className="row-actions">
                    <button type="button" onClick={() => openStockChart(stock)} disabled={isBusy || !stock.symbolToken}>
                      Chart
                    </button>
                    <button type="button" className="secondary" onClick={() => void handleDeleteSelectedWatchlistStock(stock)} disabled={isBusy}>
                      Remove
                    </button>
                  </div>
                </article>
              ))}
            </div>
          </div>
        </section>
      )}

      {page === "tradeplans" && (
        <section className="plan-panel">
          <div className="section-heading">
            <div>
              <p className="eyebrow">Trade Plan</p>
              <h2>{tradePlanForm.id ? "Edit plan" : "New plan"}</h2>
            </div>
            {tradePlanForm.id && (
              <button
                type="button"
                className="secondary"
                onClick={() => {
                  setTradePlanForm(emptyTradePlan);
                  setTradePlanStockSearch("");
                  setTradePlanStockSearchResults([]);
                }}
              >
                New plan
              </button>
            )}
          </div>

          <form className="stock-plan-form trade-plan-form" onSubmit={handleTradePlanSubmit}>
            <div className="stock-search-field">
              <label>
                Search stock
                <input
                  value={tradePlanStockSearch}
                  onChange={(event) => setTradePlanStockSearch(event.target.value)}
                  placeholder="RELIANCE"
                />
              </label>
              <label>
                Exchange
                <select
                  value={tradePlanForm.exchange}
                  onChange={(event) => {
                    setTradePlanForm((current) => ({
                      ...current,
                      exchange: event.target.value,
                      symbol: "",
                      symbolToken: "",
                      tradingSymbol: ""
                    }));
                    setTradePlanStockSearchResults([]);
                  }}
                >
                  <option value="NSE">NSE</option>
                  <option value="BSE">BSE</option>
                </select>
              </label>
              <button type="button" onClick={() => void handleTradePlanStockSearch()} disabled={isTradePlanStockSearching}>
                {isTradePlanStockSearching ? "Searching..." : "Search"}
              </button>
            </div>

            {tradePlanStockSearchResults.length > 0 && (
              <div className="stock-search-results">
                {tradePlanStockSearchResults.map((stock) => (
                  <button type="button" key={`${stock.exchange}-${stock.symbolToken}`} onClick={() => selectTradePlanStock(stock)}>
                    <strong>{stock.tradingSymbol || stock.symbol}</strong>
                    <span>
                      {stock.exchange} - {stock.symbolToken}
                    </span>
                  </button>
                ))}
              </div>
            )}

            {tradePlanForm.symbolToken && (
              <div className="selected-stock-summary">
                <div>
                  <span>Symbol</span>
                  <strong>{tradePlanForm.symbol}</strong>
                </div>
                <div>
                  <span>Trading symbol</span>
                  <strong>{tradePlanForm.tradingSymbol || "-"}</strong>
                </div>
                <div>
                  <span>Exchange</span>
                  <strong>{tradePlanForm.exchange}</strong>
                </div>
                <div>
                  <span>Token</span>
                  <strong>{tradePlanForm.symbolToken}</strong>
                </div>
              </div>
            )}

            <label>
              Buy price
              <input
                type="number"
                step="0.05"
                value={tradePlanForm.buyPrice || ""}
                onChange={(event) => setTradePlanForm((current) => ({ ...current, buyPrice: Number(event.target.value) }))}
                placeholder="0.00"
                required
              />
            </label>
            <label>
              Sell price
              <input
                type="number"
                step="0.05"
                value={tradePlanForm.sellPrice || ""}
                onChange={(event) => setTradePlanForm((current) => ({ ...current, sellPrice: Number(event.target.value) }))}
                placeholder="0.00"
                required
              />
            </label>
            <label>
              Quantity
              <input
                type="number"
                step="1"
                min="1"
                value={tradePlanForm.quantity || ""}
                onChange={(event) => setTradePlanForm((current) => ({ ...current, quantity: Number(event.target.value) }))}
                placeholder="1"
                required
              />
            </label>
            <label>
              Max budget
              <input
                type="number"
                step="0.05"
                value={tradePlanForm.maxBudget ?? ""}
                onChange={(event) =>
                  setTradePlanForm((current) => ({
                    ...current,
                    maxBudget: event.target.value ? Number(event.target.value) : null
                  }))
                }
                placeholder="Optional"
              />
            </label>
            <label>
              Status
              <select
                value={tradePlanForm.status || "Active"}
                onChange={(event) => setTradePlanForm((current) => ({ ...current, status: event.target.value }))}
              >
                <option value="Active">Active</option>
                <option value="Paused">Paused</option>
                <option value="Cancelled">Cancelled</option>
              </select>
            </label>
            <label className="checkbox-field">
              <input
                type="checkbox"
                checked={tradePlanForm.isActive}
                onChange={(event) => setTradePlanForm((current) => ({ ...current, isActive: event.target.checked }))}
              />
              Active
            </label>
            <label className="checkbox-field">
              <input
                type="checkbox"
                checked={tradePlanForm.repeatEnabled}
                onChange={(event) => setTradePlanForm((current) => ({ ...current, repeatEnabled: event.target.checked }))}
              />
              Repeat
            </label>
            <button type="submit" disabled={isBusy}>
              {tradePlanForm.id ? "Update plan" : "Save plan"}
            </button>
          </form>

          <div className="planned-stocks trade-plan-list">
            {tradePlans.map((tradePlan) => (
              <article key={tradePlan.id || `${tradePlan.exchange}-${tradePlan.symbolToken}`}>
                <div>
                  <strong>{tradePlan.tradingSymbol || tradePlan.symbol}</strong>
                  <span>
                    {tradePlan.exchange} - {tradePlan.symbolToken}
                  </span>
                </div>
                <div>
                  <span>Buy</span>
                  <strong>{formatMoney(tradePlan.buyPrice)}</strong>
                </div>
                <div>
                  <span>Sell</span>
                  <strong>{formatMoney(tradePlan.sellPrice)}</strong>
                </div>
                <div>
                  <span>Qty</span>
                  <strong>{tradePlan.quantity}</strong>
                </div>
                <div>
                  <span>Status</span>
                  <strong>{tradePlan.status || (tradePlan.isActive ? "Active" : "Paused")}</strong>
                </div>
                <div className="row-actions">
                  <button type="button" className="secondary" onClick={() => handleEditTradePlan(tradePlan)} disabled={isBusy}>
                    Edit
                  </button>
                  <button
                    type="button"
                    className="secondary"
                    onClick={() => tradePlan.id && void handleDeleteTradePlan(tradePlan.id)}
                    disabled={isBusy || !tradePlan.id}
                  >
                    Remove
                  </button>
                </div>
              </article>
            ))}
          </div>
        </section>
      )}

      {chartStock && (
        <div className="modal-backdrop" role="presentation">
          <section className="chart-modal" role="dialog" aria-modal="true" aria-label="Stock chart">
            <div className="chart-modal-header">
              <div>
                <p className="eyebrow">{chartStock.exchange}</p>
                <h2>{chartStock.tradingSymbol || chartStock.symbol}</h2>
              </div>
              <button type="button" className="secondary" onClick={closeStockChart}>
                Close
              </button>
            </div>

            <div className="chart-range-tabs">
              {chartRanges.map((range) => (
                <button
                  type="button"
                  className={chartRange === range.value ? "active" : ""}
                  key={range.value}
                  onClick={() => handleChartRangeChange(range.value)}
                  disabled={isChartLoading}
                >
                  {range.label}
                </button>
              ))}
            </div>

            <div className="chart-surface">
              {isChartLoading && <p className="helper-text">Loading chart...</p>}
              {!isChartLoading && chartCandles.length === 0 && <p className="helper-text">No chart data available.</p>}
              {!isChartLoading && chartCandles.length > 0 && <StockLineChart candles={chartCandles} />}
            </div>
          </section>
        </div>
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
