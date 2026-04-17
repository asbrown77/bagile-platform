import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { TrelloAdapter } from "./TrelloAdapter.js";

function makeResponse(status: number, body: unknown, statusText = "OK"): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    statusText,
    json: async () => body,
    headers: {
      get: (name: string) =>
        name.toLowerCase() === "content-type" ? "application/json" : null,
    },
  } as unknown as Response;
}

const BOARD_ID = "hNs49hi4";

describe("TrelloAdapter", () => {
  let adapter: TrelloAdapter;

  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn());
    adapter = new TrelloAdapter("test-key", "test-token");
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("calls correct Trello URLs with API key and token params", async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(makeResponse(200, []))  // cards
      .mockResolvedValueOnce(makeResponse(200, [])); // lists

    await adapter.getOpenCards(BOARD_ID);

    expect(fetch).toHaveBeenCalledTimes(2);

    const cardsUrl = vi.mocked(fetch).mock.calls[0][0] as string;
    expect(cardsUrl).toContain(`/1/boards/${BOARD_ID}/cards`);
    expect(cardsUrl).toContain("filter=open");
    expect(cardsUrl).toContain("key=test-key");
    expect(cardsUrl).toContain("token=test-token");

    const listsUrl = vi.mocked(fetch).mock.calls[1][0] as string;
    expect(listsUrl).toContain(`/1/boards/${BOARD_ID}/lists`);
    expect(listsUrl).toContain("filter=open");
    expect(listsUrl).toContain("key=test-key");
    expect(listsUrl).toContain("token=test-token");
  });

  it("maps cards with list names and isOverdue calculation", async () => {
    const pastDate = "2020-01-01T00:00:00.000Z";
    const futureDate = "2099-12-31T00:00:00.000Z";

    const cards = [
      { id: "c1", name: "Acme — Jane", idList: "list1", due: pastDate, url: "https://trello.com/c/c1" },
      { id: "c2", name: "Beta — Bob", idList: "list2", due: futureDate, url: "https://trello.com/c/c2" },
      { id: "c3", name: "Gamma — Carol", idList: "list1", due: null, url: "https://trello.com/c/c3" },
    ];
    const lists = [
      { id: "list1", name: "Incoming" },
      { id: "list2", name: "Quoting" },
    ];

    vi.mocked(fetch)
      .mockResolvedValueOnce(makeResponse(200, cards))
      .mockResolvedValueOnce(makeResponse(200, lists));

    const result = await adapter.getOpenCards(BOARD_ID);

    expect(result).toHaveLength(3);

    expect(result[0]).toEqual({
      id: "c1",
      name: "Acme — Jane",
      listName: "Incoming",
      dueDate: pastDate,
      isOverdue: true,
      url: "https://trello.com/c/c1",
    });

    expect(result[1]).toEqual({
      id: "c2",
      name: "Beta — Bob",
      listName: "Quoting",
      dueDate: futureDate,
      isOverdue: false,
      url: "https://trello.com/c/c2",
    });

    expect(result[2]).toEqual({
      id: "c3",
      name: "Gamma — Carol",
      listName: "Incoming",
      dueDate: undefined,
      isOverdue: false,
      url: "https://trello.com/c/c3",
    });
  });

  it("handles empty board gracefully", async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(makeResponse(200, []))
      .mockResolvedValueOnce(makeResponse(200, []));

    const result = await adapter.getOpenCards(BOARD_ID);

    expect(result).toEqual([]);
  });
});
