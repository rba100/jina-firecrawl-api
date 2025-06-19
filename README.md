# Jina Firecrawl API Shim

This project exposes an API compatible with the Firecrawl `/v1/scrape` endpoint. It uses Jina.ai for scraping general URLs and `MarkItDown` for processing PDF files.

It'd intended to be a drop in replacement for Firecrawl for LibreChat users.

## Features

*   **Firecrawl Compatible**: Implements the `/v1/scrape` endpoint with a similar request and response structure.
*   **Jina.ai Integration**: Leverages Jina.ai's `r.jina.ai` service for scraping content from web pages.
*   **PDF Processing**: Uses `MarkItDown` to convert PDF documents (under 2MB) to markdown.
*   **Dynamic Handler**: Automatically switches between Jina.ai for general URLs and MarkItDown for `.pdf` URLs.
*   **Dockerized**: Includes `Dockerfile` and `docker-compose.yml` for easy deployment.

## Prerequisites

*   Python 3.8+
*   Docker & Docker Compose (Recommended for running)
*   A Jina AI API Key

## Setup and Configuration

1.  **Clone the repository:**
    ```bash
    git clone <repository_url>
    cd jina-firecrawl-api
    ```

2.  **Create a virtual environment (optional but recommended):**
    ```bash
    python3 -m venv venv
    source venv/bin/activate
    ```

3.  **Install dependencies:**
    ```bash
    pip install -r requirements.txt
    ```
## API Key

Supply an https://jina.ai key as the bearer auth token. This will be used when the request is serviced by jina.

## Running the Application

You can run the application locally, with Docker, or with Docker Compose.

### 1. Locally (for development)

```bash
uvicorn app:app --host 0.0.0.0 --port 3002 --reload
```
The API will be available at `http://localhost:3002`.

### 2. With Docker

First, build the Docker image:
```bash
docker build -t jina-firecrawl-api .
```

Then, run the container, passing the `JINA_API_KEY` as an environment variable:
```bash
docker run -p 3002:3002 jina-firecrawl-api
```

### 3. With Docker Compose

Ensure you have created the `.env` file in the project root as described in the "Setup and Configuration" section.

Then, simply run:
```bash
docker-compose up
```
This will build the image (if not already built) and run the service. The API will be available at `http://localhost:3002`.

## API Endpoints

### `POST /v1/scrape`

This endpoint scrapes a given URL and returns its content as markdown.

*   **Request Body (JSON):**
    ```json
    {
      "url": "<URL_TO_SCRAPE>"
    }
    ```

*   **Behavior:**
    *   If the URL ends with `.pdf`, the content is fetched, and if it's under 2MB, it's converted to markdown using `MarkItDown`.
    *   For other URLs, Jina.ai is used to fetch and convert the content to markdown.

*   **Success Response (200 OK):**
    ```json
    {
      "success": true,
      "data": {
        "markdown": "<markdown_content_string>",
        "html": "" 
      },
      "metadata": {
        "sourceURL": "<original_url>",
        "title": "<original_url>",
        "description": "Scraped content",
        "language": "en",
        "statusCode": 200
      }
    }
    ```
    *Note: `html` field is currently always an empty string. `title` and `description` are basic placeholders.*

*   **Error Response (e.g., 413 for large PDFs, 500 for Jina errors):**
    ```json
    {
      "success": false,
      "data": null,
      "metadata": {
        "sourceURL": "<original_url>",
        "statusCode": <http_error_code>
      }
    }
    ```

### `POST /v1/scrape_raw`

This is a debugging endpoint. It logs the raw JSON request body to the console and then intentionally returns an HTTP 500 error.

*   **Request Body (JSON):**
    Any valid JSON.
    ```json
    {
      "url": "<URL_TO_SCRAPE>",
      "some_other_data": "test"
    }
    ```
*   **Response (500 Internal Server Error):**
    ```json
    {
        "detail": "Intentional server error for /v1/scrape_raw"
    }
    ```
