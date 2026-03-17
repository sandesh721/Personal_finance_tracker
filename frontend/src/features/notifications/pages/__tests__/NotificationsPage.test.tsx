import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { vi } from "vitest";

const useAuthMock = vi.fn();
const notificationsListMock = vi.fn();
const notificationsMarkReadMock = vi.fn();
const notificationsMarkAllReadMock = vi.fn();

vi.mock("../../../../app/providers/AuthProvider", () => ({
  useAuth: () => useAuthMock(),
}));

vi.mock("../../api/notificationsApi", () => ({
  notificationsApi: {
    list: (...args: unknown[]) => notificationsListMock(...args),
    markRead: (...args: unknown[]) => notificationsMarkReadMock(...args),
    markAllRead: (...args: unknown[]) => notificationsMarkAllReadMock(...args),
  },
}));

import { NotificationsPage } from "../NotificationsPage";

describe("NotificationsPage", () => {
  it("loads notifications and marks one as read when opened", async () => {
    useAuthMock.mockReturnValue({ accessToken: "token" });
    notificationsListMock.mockResolvedValue({
      unreadCount: 1,
      items: [
        {
          id: "n1",
          type: "RecurringDueReminder",
          level: "Info",
          title: "Recurring reminder: Rent",
          message: "Review Rent before it runs.",
          route: "/recurring",
          isRead: false,
          createdUtc: "2026-03-17T00:00:00Z",
          readAtUtc: null,
        },
      ],
    });
    notificationsMarkReadMock.mockResolvedValue(undefined);
    notificationsMarkAllReadMock.mockResolvedValue(undefined);

    render(
      <MemoryRouter>
        <NotificationsPage />
      </MemoryRouter>,
    );

    expect(await screen.findByText(/notification history/i)).toBeInTheDocument();
    await userEvent.click(screen.getByRole("button", { name: /recurring reminder: rent/i }));

    await waitFor(() => expect(notificationsMarkReadMock).toHaveBeenCalledWith("token", "n1"));
  });
});
