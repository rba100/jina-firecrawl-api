from fastapi import FastAPI, HTTPException, Request
from pydantic import BaseModel
import httpx
import uvicorn
import logging
import os
from jina_handler import scrape_with_jina # Import the refactored Jina handler
from pdf_handler import scrape_pdf_with_markitdown # Import the new PDF handler

logger = logging.getLogger("api")

app = FastAPI()

class ScrapeRequest(BaseModel):
    url: str

class FirecrawlData(BaseModel):
    markdown: str
    html: str | None = ""

class FirecrawlMetadata(BaseModel):
    sourceURL: str
    title: str | None = None
    description: str | None
    language: str | None = "en"
    statusCode: int | None

class FirecrawlResponse(BaseModel):
    success: bool
    data: FirecrawlData | None = None
    metadata: FirecrawlMetadata | None = None

class FirecrawlErrorResponse(BaseModel):
    error: str

jina_api_key = os.getenv("JINA_API_KEY")
if not jina_api_key:
    raise ValueError("JINA_API_KEY environment variable is not set. Please set it to use the Jina API.")

SERVICE_TIMEOUT_S = int(os.getenv("SERVICE_TIMEOUT_S", "10"))

@app.post("/v1/scrape", response_model=FirecrawlResponse)
async def scrape_url(request: ScrapeRequest):
    source_url = request.url
    markdown_content = ""
    status_code = 200 # Default success status code

    print(f"Received scrape request for URL: {source_url}")

    try:
        if source_url.lower().endswith(".pdf"):
            if not source_url.startswith(("http://", "https://")):
                 raise HTTPException(status_code=400, detail="Invalid URL scheme for PDF. Must be http or https.")
            markdown_content = await scrape_pdf_with_markitdown(source_url, timeout_seconds=SERVICE_TIMEOUT_S)
        else:
            if not jina_api_key: # Check if API key is available before calling Jina
                raise HTTPException(status_code=500, detail="JINA_API_KEY is not configured, cannot scrape non-PDF URLs.")
            markdown_content = await scrape_with_jina(source_url, jina_api_key, timeout_seconds=SERVICE_TIMEOUT_S)

        return FirecrawlResponse(
            success=True,
            data=FirecrawlData(markdown=markdown_content, html=""), # Assuming html is not populated by these handlers
            metadata=FirecrawlMetadata(
                sourceURL=source_url, 
                title=source_url, # Basic title
                description="Scraped content", # Basic description
                language="en", 
                statusCode=status_code
            )
        )
    except httpx.HTTPStatusError as e:
        # Log the error for debugging
        print(f"HTTPStatusError for {source_url}: {e.response.status_code} - {e.response.text}")
        return FirecrawlResponse(
            success=False,
            data=FirecrawlData(markdown="This link cannot be read programmatically", html=""),
            metadata=FirecrawlMetadata(
                sourceURL=source_url,
                title="This link cannot be read programmatically", # No title available on error
                description="", # No description available on error
                language="en", # No language available on error
                statusCode=e.response.status_code,
                # error=f"Failed to fetch/process: {e.response.status_code}" # Consider adding error field to metadata
            )
        )
    except httpx.RequestError as e:
        print(f"RequestError for {source_url}: {str(e)}")
        return FirecrawlResponse(
            success=False,
            data=FirecrawlData(markdown="This link cannot be read programmatically", html=""),
            metadata=FirecrawlMetadata(
                sourceURL=source_url,
                title="This link cannot be read programmatically", # No title available on error
                description="",
                language="en",
                statusCode=503,
            )
        )
    except HTTPException as e: # Catch HTTPExceptions raised explicitly (like missing API key or bad PDF URL)
        return FirecrawlResponse(
            success=False,
            data=None,
            metadata=FirecrawlMetadata(
                sourceURL=source_url,
                statusCode=e.status_code,
                # error=e.detail
            )
        )
    except Exception as e:
        print(f"Unexpected error for {source_url}: {str(e)}")
        # Log the full traceback for debugging
        import traceback
        traceback.print_exc()
        return FirecrawlResponse(
            success=False,
            data=None,
            metadata=FirecrawlMetadata(
                sourceURL=source_url,
                statusCode=500, # Internal Server Error
                # error=f"An unexpected error occurred: {str(e)}"
            )
        )

@app.post("/v1/scrape_raw")
async def scrape_raw(request: Request):
    try:
        request_body = await request.json()
        print(f"Received request on /v1/scrape_raw: {request_body}")
    except Exception as e:
        print(f"Error reading JSON body from /v1/scrape_raw: {e}")
    raise HTTPException(status_code=500, detail="Intentional server error for /v1/scrape_raw")

if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=os.getenv("PORT", 3002))
