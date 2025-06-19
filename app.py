from fastapi import FastAPI, HTTPException, Request
from fastapi.responses import JSONResponse
from pydantic import BaseModel
from typing import Union
import httpx
import uvicorn
import logging
import os
from jina_handler import scrape_with_jina
from pdf_handler import scrape_pdf_with_markitdown

logger = logging.getLogger("api")

app = FastAPI()

class ScrapeRequest(BaseModel):
    url: str

class FirecrawlData(BaseModel):
    markdown: str
    html: str = ""

class FirecrawlMetadata(BaseModel):
    sourceURL: str
    title: str = ""
    description: str = ""
    language: str = ""
    statusCode: int = 200

class FirecrawlResponse(BaseModel):
    success: bool = True
    data: FirecrawlData
    metadata: FirecrawlMetadata

class FirecrawlErrorResponse(BaseModel):
    error: str

SERVICE_TIMEOUT_S = int(os.getenv("SERVICE_TIMEOUT_S", "10"))

@app.post("/v1/scrape", response_model=Union[FirecrawlResponse, FirecrawlErrorResponse], responses={
    200: {"model": FirecrawlResponse},
    400: {"model": FirecrawlErrorResponse},
    500: {"model": FirecrawlErrorResponse},
    503: {"model": FirecrawlErrorResponse},
})
async def scrape_url(scrape_request: ScrapeRequest, request: Request):
    source_url = scrape_request.url
    markdown_content = ""
    # status_code will be part of metadata for success, or the response status for errors

    print(f"Received scrape request for URL: {source_url}")

    try:
        if source_url.lower().endswith(".pdf"):
            if not source_url.startswith(("http://", "https://")):
                 # This will be caught by the HTTPException handler below
                 raise HTTPException(status_code=400, detail="Invalid URL scheme for PDF. Must be http or https.")
            markdown_content = await scrape_pdf_with_markitdown(source_url, timeout_seconds=SERVICE_TIMEOUT_S)
            if not markdown_content: # Handle case where PDF scraping might return empty
                raise HTTPException(status_code=500, detail="Failed to extract content from PDF.")
        else:
            auth_header = request.headers.get("Authorization")
            if not auth_header:
                raise HTTPException(status_code=401, detail="Authorization header required for non-PDF URLs")
            markdown_content = await scrape_with_jina(source_url, auth_header, timeout_seconds=SERVICE_TIMEOUT_S)
            if not markdown_content: # Handle case where Jina might return empty
                raise HTTPException(status_code=500, detail="Failed to extract content using Jina.")


        return FirecrawlResponse(
            data=FirecrawlData(markdown=markdown_content, html=""),
            metadata=FirecrawlMetadata(
                sourceURL=source_url,
                title=source_url, # Basic title, can be improved
                description="Scraped content", # Basic description
                language="en", # Or try to detect
                statusCode=200
            )
        )
    except httpx.HTTPStatusError as e:
        error_message = f"Failed to fetch/process URL. Upstream status: {e.response.status_code}"
        print(f"HTTPStatusError for {source_url}: {e.response.status_code} - {e.response.text}")
        return JSONResponse(
            status_code=e.response.status_code if e.response.status_code >= 400 else 500, # Ensure client/server error
            content=FirecrawlErrorResponse(error=error_message).model_dump()
        )
    except httpx.RequestError as e:
        error_message = f"Request failed for URL: {str(e)}"
        print(f"RequestError for {source_url}: {str(e)}")
        return JSONResponse(
            status_code=503, # Service Unavailable
            content=FirecrawlErrorResponse(error=error_message).model_dump()
        )
    except HTTPException as e: # Catch our own explicit HTTPExceptions
        return JSONResponse(
            status_code=e.status_code,
            content=FirecrawlErrorResponse(error=e.detail).model_dump()
        )
    except Exception as e:
        error_message = f"An unexpected server error occurred: {str(e)}"
        print(f"Unexpected error for {source_url}: {str(e)}")
        import traceback
        traceback.print_exc()
        return JSONResponse(
            status_code=500, # Internal Server Error
            content=FirecrawlErrorResponse(error=error_message).model_dump()
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
