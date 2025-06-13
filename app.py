from fastapi import FastAPI, HTTPException, Request
from pydantic import BaseModel
import httpx
import uvicorn
import logging
import os

app = FastAPI()

class ScrapeRequest(BaseModel):
    url: str

class FirecrawlData(BaseModel):
    markdown: str
    html: str | None = ""

class FirecrawlMetadata(BaseModel):
    sourceURL: str
    # Add other metadata fields here if needed in the future
    title: str | None = None
    description: str | None
    language: str | None = "en"
    statusCode: int | None
    # error: str | None = None

class FirecrawlResponse(BaseModel):
    success: bool
    data: FirecrawlData | None = None
    metadata: FirecrawlMetadata | None = None # Allow metadata to be optional for error cases

jina_api_key = os.getenv("JINA_API_KEY", None)
if not jina_api_key:
    raise ValueError("JINA_API_KEY environment variable is not set. Please set it to use the Jina API.")

@app.post("/v1/scrape", response_model=FirecrawlResponse)
async def scrape_url(request: ScrapeRequest):
    jina_url = f"https://r.jina.ai/{request.url}"
    try:
        async with httpx.AsyncClient() as client:
            headers = {
                "Authorization": f"Bearer {jina_api_key}"
            }
            try:
                response = await client.get(jina_url, headers=headers, follow_redirects=True)
            except Exception as e:
                # Log the error for debugging
                print(f"Error fetching from Jina.ai: {e}")
                raise HTTPException(status_code=500, detail="Failed to fetch from Jina.ai")

            response.raise_for_status()  # Raise an exception for bad status codes (4xx or 5xx)
        
        markdown_content = response.text

        return FirecrawlResponse(
            success=True,
            data=FirecrawlData(markdown=markdown_content, html=""),
            metadata=FirecrawlMetadata(sourceURL=request.url, title=request.url, description=request.url, language="en", statusCode=200)
        )
    except httpx.HTTPStatusError as e:
        # Forward the status code and error if possible, or use a generic error
        return FirecrawlResponse(
            success=False,
            data=None,
            metadata=FirecrawlMetadata(
                sourceURL=request.url
                # statusCode=e.response.status_code, # Consider adding this if you expand metadata
                # error=f"Failed to fetch from Jina.ai: {e.response.status_code} {e.response.reason_phrase}"
            )
        )
    except httpx.RequestError as e:
        # Handle network errors or other request issues
        return FirecrawlResponse(
            success=False,
            data=None,
            metadata=FirecrawlMetadata(
                sourceURL=request.url
                # error=f"Request to Jina.ai failed: {str(e)}"
            )
        )
    except Exception as e:
        # Catch-all for any other unexpected errors
        # Log the error for debugging: print(f"Unexpected error: {e}")
        return FirecrawlResponse(
            success=False,
            data=None,
            metadata=FirecrawlMetadata(
                sourceURL=request.url
                # error=f"An unexpected error occurred: {str(e)}"
            )
        )

if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=3002)
