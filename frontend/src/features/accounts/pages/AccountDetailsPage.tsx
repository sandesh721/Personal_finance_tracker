import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useAuth } from "../../../app/providers/AuthProvider";
import { accountsApi, type AccountDto } from "../api/accountsApi";
import { transactionsApi, type TransactionDto } from "../../transactions/api/transactionsApi";
import { goalsApi, type GoalDto } from "../../goals/api/goalsApi";
import { recurringTransactionsApi, type RecurringTransactionDto } from "../../recurring/api/recurringTransactionsApi";
import { Alert } from "../../../shared/components/Alert";
import { EmptyState } from "../../../shared/components/EmptyState";
import { PageLoader } from "../../../shared/components/PageLoader";
import { ProgressBar } from "../../../shared/components/ProgressBar";
import { SectionHeader } from "../../../shared/components/SectionHeader";
import { ApiError } from "../../../shared/lib/api/client";
import { formatCurrency, formatDate } from "../../../shared/lib/format";

const typeLabels: Record<AccountDto["type"], string> = {
  BankAccount: "Bank account",
  CreditCard: "Credit card",
  CashWallet: "Cash wallet",
  SavingsAccount: "Savings account",
};

export function AccountDetailsPage() {
  const { accountId } = useParams();
  const navigate = useNavigate();
  const { accessToken } = useAuth();
  const [account, setAccount] = useState<AccountDto | null>(null);
  const [transactions, setTransactions] = useState<TransactionDto[]>([]);
  const [linkedGoals, setLinkedGoals] = useState<GoalDto[]>([]);
  const [recurringRules, setRecurringRules] = useState<RecurringTransactionDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    void load();
  }, [accessToken, accountId]);

  async function load() {
    if (!accessToken || !accountId) return;

    setLoading(true);
    try {
      const [accountResponse, transactionResponse, goals, recurring] = await Promise.all([
        accountsApi.get(accessToken, accountId),
        transactionsApi.list(accessToken, { accountId, page: 1, pageSize: 8 }),
        goalsApi.list(accessToken),
        recurringTransactionsApi.list(accessToken),
      ]);

      setAccount(accountResponse);
      setTransactions(transactionResponse.items);
      setLinkedGoals(goals.filter((goal) => goal.linkedAccountId === accountId));
      setRecurringRules(recurring.filter((rule) => rule.accountId === accountId || rule.transferAccountId === accountId));
      setErrorMessage(null);
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Unable to load account details.");
    } finally {
      setLoading(false);
    }
  }

  const netChange = useMemo(() => (account ? account.currentBalance - account.openingBalance : 0), [account]);
  const recentIncome = useMemo(() => transactions.filter((item) => item.type === "Income").reduce((total, item) => total + item.amount, 0), [transactions]);
  const recentExpense = useMemo(() => transactions.filter((item) => item.type === "Expense").reduce((total, item) => total + item.amount, 0), [transactions]);

  if (loading) return <PageLoader label="Loading account details" />;
  if (!account) return <Alert message={errorMessage ?? "Account details are unavailable."} />;

  return (
    <div className="page-stack">
      <SectionHeader
        title={account.name}
        description="Review this account's balance, recent ledger activity, linked goals, and recurring rules in one place."
        action={
          <div className="section-header__actions">
            <button type="button" className="ghost-button" onClick={() => navigate("/accounts")}>Back to accounts</button>
            <Link to="/transactions" className="ghost-button account-details__link-button">All transactions</Link>
          </div>
        }
      />
      {errorMessage ? <Alert message={errorMessage} /> : null}

      <section className="panel-card account-details-hero">
        <div className="account-details-hero__summary">
          <div>
            <p className="eyebrow">Account overview</p>
            <h3>{typeLabels[account.type]} • {account.currencyCode}</h3>
            <p>{account.institutionName || "Personal ledger"}{account.last4Digits ? ` • •••• ${account.last4Digits}` : ""}</p>
          </div>
          <div className="account-details-hero__balance">
            <span>Current balance</span>
            <strong>{formatCurrency(account.currentBalance, account.currencyCode)}</strong>
            <small>{account.isArchived ? "Archived account" : "Active account"}</small>
          </div>
        </div>
        <div className="stats-grid stats-grid--four">
          <article className="stat-card">
            <p>Opening balance</p>
            <strong>{formatCurrency(account.openingBalance, account.currencyCode)}</strong>
            <span>Initial ledger position.</span>
          </article>
          <article className={`stat-card ${netChange >= 0 ? "stat-card--positive" : "stat-card--negative"}`}>
            <p>Net change</p>
            <strong>{formatCurrency(netChange, account.currencyCode)}</strong>
            <span>Current balance minus opening balance.</span>
          </article>
          <article className="stat-card">
            <p>Linked goals</p>
            <strong>{linkedGoals.length}</strong>
            <span>{linkedGoals.length === 0 ? "No goals linked yet." : "Savings targets using this account."}</span>
          </article>
          <article className="stat-card">
            <p>Recurring rules</p>
            <strong>{recurringRules.length}</strong>
            <span>{recurringRules.length === 0 ? "No recurring rules tied here." : "Automation affecting this account."}</span>
          </article>
        </div>
      </section>

      <div className="account-details-layout">
        <section className="panel-card">
          <div className="panel-card__header">
            <h3>Recent activity</h3>
            <p>Latest ledger entries affecting this account.</p>
          </div>
          {transactions.length === 0 ? (
            <EmptyState title="No activity yet" description="Transactions tied to this account will appear here once recorded." />
          ) : (
            <div className="simple-list">
              {transactions.map((item) => (
                <div key={item.id} className="list-row list-row--transaction-activity">
                  <div className={`activity-dot activity-dot--${item.type.toLowerCase()}`} />
                  <div className="transaction-activity__body">
                    <div className="transaction-activity__topline">
                      <strong className="transaction-activity__title">{item.merchant || item.categoryName || item.type}</strong>
                      <strong className="transaction-activity__amount-value">{formatCurrency(item.amount, account.currencyCode)}</strong>
                    </div>
                    <div className="transaction-activity__meta">
                      <span className={`transaction-type-pill transaction-type-pill--${item.type.toLowerCase()}`}>{item.type}</span>
                      <span>{item.categoryName || "Uncategorised"}</span>
                      <span>{formatDate(item.dateUtc)}</span>
                    </div>
                    {item.note ? <p className="transaction-activity__note">{item.note}</p> : null}
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>

        <section className="panel-card">
          <div className="panel-card__header">
            <h3>Account pulse</h3>
            <p>Quick financial context from recent recorded activity.</p>
          </div>
          <div className="account-details-pulse">
            <div className="metric-card">
              <span>Recent income</span>
              <strong>{formatCurrency(recentIncome, account.currencyCode)}</strong>
              <p>Income transactions in the latest loaded activity for this account.</p>
            </div>
            <div className="metric-card">
              <span>Recent expense</span>
              <strong>{formatCurrency(recentExpense, account.currencyCode)}</strong>
              <p>Expense transactions in the latest loaded activity for this account.</p>
            </div>
          </div>
        </section>
      </div>

      <div className="account-details-layout">
        <section className="panel-card">
          <div className="panel-card__header">
            <h3>Linked goals</h3>
            <p>Savings goals that contribute to or withdraw from this account.</p>
          </div>
          {linkedGoals.length === 0 ? (
            <EmptyState title="No linked goals" description="When a goal is tied to this account, it will appear here." />
          ) : (
            <div className="simple-list">
              {linkedGoals.map((goal) => (
                <div key={goal.id} className="list-row list-row--stacked">
                  <div>
                    <strong>{goal.name}</strong>
                    <p>{formatCurrency(goal.currentAmount)} saved of {formatCurrency(goal.targetAmount)}</p>
                  </div>
                  <div className="dashboard-inline-progress">
                    <ProgressBar value={goal.progressPercent} tone={goal.status === "Completed" ? "warning" : "default"} />
                    <small>{goal.progressPercent.toFixed(2)}% • {goal.targetDateUtc ? formatDate(goal.targetDateUtc) : "Flexible date"}</small>
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>

        <section className="panel-card">
          <div className="panel-card__header">
            <h3>Recurring rules</h3>
            <p>Scheduled rules that debit, credit, or transfer through this account.</p>
          </div>
          {recurringRules.length === 0 ? (
            <EmptyState title="No recurring rules" description="Recurring items tied to this account will appear here." />
          ) : (
            <div className="simple-list">
              {recurringRules.map((rule) => (
                <div key={rule.id} className="list-row list-row--stacked">
                  <div>
                    <strong>{rule.title}</strong>
                    <p>{rule.frequency} • {rule.categoryName || rule.type}</p>
                  </div>
                  <div className="account-details-rule-aside">
                    <strong>{formatCurrency(rule.amount)}</strong>
                    <small>{rule.nextRunDateUtc ? `Next ${formatDate(rule.nextRunDateUtc)}` : "No next run scheduled"}</small>
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>
      </div>
    </div>
  );
}
