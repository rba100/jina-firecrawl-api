## Jina Firecrawl API Replacement for LibreChat

This project is a drop-in replacement for Firecrawl, designed for LibreChat, but powered by Jina.AI. It exposes a `/v1/scrape` endpoint compatible with Firecrawl's API.

### How it works

- For most URLs, requests are proxied to Jina.AI's [r.jina.ai](https://r.jina.ai) service. You must provide your Jina API key as the Bearer token in the `Authorization` header; this key is forwarded to Jina for those requests.
- For PDF URLs, the service downloads and parses the PDF directly. Any API key is accepted for PDF requests.

### Usage

In Librechat, set `FIRECRAWL_API_URL` to `http://localhost:3002/` or wherever you will host this, and `FIRECRAWL_API_KEY` to your Jina.AI key.

**Run directly from GitHub Container Registry (recommended):**

```sh
docker run -d -p 3002:8080 ghcr.io/rba100/jina-firecrawl-api:master
```

This will pull and run the latest published image from [ghcr.io](https://ghcr.io/). It needs no configuration - just use your jina api key with it.

**Or run with Docker Compose (builds locally):**

```sh
docker compose up -d
```

**Or run directly (requires .NET 9):**

```sh
dotnet run
```

The API will be available at `http://localhost:3002` by default.

### Configuration

#### Timeout

You can configure the timeout for scraping operations (in seconds) via either `appsettings.json` or the `SCRAPE__TIMEOUTSECONDS` environment variable. The default is 15 seconds.

- **Environment variable:**  
  Set `SCRAPE__TIMEOUTSECONDS` (note the double underscore) to your desired timeout value.  
  This is the .NET convention for mapping environment variables to configuration sections and properties (e.g., `Scrape:TimeoutSeconds` in appsettings.json maps to `SCRAPE__TIMEOUTSECONDS` as an environment variable).

- **appsettings.json:**  
  Add or edit the following section:
  ```json
  "Scrape": {
    "TimeoutSeconds": 20
  }
  ```

This timeout also controls the "fallback" timeout passed to Jina. If a page takes too long to load with JavaScript execution, Jina will abort the browser-based scrape and fall back to scraping the raw HTML (without JS execution). This fallback is much faster, but may compromise accuracy for pages that are slow to load or require JavaScript for rendering.

**Tradeoff:**  
The fallback feature improves speed for slow or problematic pages, but may result in incomplete or less accurate content for sites that require JavaScript to render important information.

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
