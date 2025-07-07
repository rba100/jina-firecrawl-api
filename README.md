## Jina Firecrawl API Replacement for LibreChat

This project is a drop-in replacement for Firecrawl, designed for LibreChat, but powered by Jina.AI. It exposes a `/v1/scrape` endpoint compatible with Firecrawl's API.

### How it works

- For most URLs, requests are proxied to Jina.AI's [r.jina.ai](https://r.jina.ai) service. You must provide your Jina API key as the Bearer token in the `Authorization` header; this key is forwarded to Jina for those requests.
- For PDF URLs, the service downloads and parses the PDF directly. Any API key is accepted for PDF requests.

### Usage

**Run with Docker Compose:**

```sh
docker compose up -d
```

**Or run directly (requires .NET 9):**

```sh
dotnet run
```

The API will be available at `http://localhost:3002` by default.

### Endpoint

`POST /v1/scrape`

**Request body:**

```json
{
  "url": "<URL_TO_SCRAPE>"
}
```

**Headers:**

- `Authorization: Bearer <your-jina-api-key>`

**Response:**

- On success: returns markdown (and empty html) in the `data` field, plus metadata.
- On error: returns `success: false` and error metadata.
