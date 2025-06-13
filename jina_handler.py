import httpx
import os

async def scrape_with_jina(url: str, api_key: str) -> str:
    """
    Scrapes the given URL using the Jina.ai API.
    """
    jina_url = f"https://r.jina.ai/{url}"
    async with httpx.AsyncClient() as client:
        headers = {
            "Authorization": f"Bearer {api_key}"
        }
        response = await client.get(jina_url, headers=headers, follow_redirects=True)
        response.raise_for_status()
        return response.text
