from typing import Any, Dict
import azure.functions as func
import datetime
import json
import logging
import os
from agents import Agent, Runner, function_tool, set_tracing_export_api_key

@function_tool
def record_travel_costs(amount: float):
    return "Reisekosten wurden verbucht."

@function_tool
def record_fuel_costs(amount: float):
    return "Tankkosten wurden verbucht."

@function_tool
def record_expenses(amount: float):
    return "Spesen wurden verbucht."

# ---------- Agent singleton ----------
_agent: Agent | None = None

travel_cost_agent: Agent = Agent(
    name="Travel Cost Agent",
    instructions="Du bist ein Buchhalter und bearbeitest Reisekosten. Nutze dafür die bereitgestellten Tool.",
    tools=[record_travel_costs],
)
fuel_cost_agent: Agent = Agent(
    name="Fuel Cost Agent",
    instructions="Du bist ein Buchhalter und bearbeitest Tankkosten. Nutze dafür die bereitgestellten Tool.",
    tools=[record_fuel_costs],
)
expenses_agent: Agent = Agent(
    name="Expenses Agent",
    instructions="Du bist ein Buchhalter und bearbeitest Spesenrechnungen. Nutze dafür die bereitgestellten Tool.",
    tools=[record_expenses],
)

def get_or_create_agent() -> Agent:
    global _agent
    if _agent is None:
        _agent = Agent(
            name="Triage Agent",
            instructions="Du routest den Benutzer zu einem passenden Agent. Stelle bitte keine Rückfragen an den Benutzer.",
            handoffs=[fuel_cost_agent, travel_cost_agent, expenses_agent]
        )
    return _agent


app = func.FunctionApp()

@app.function_name(name = "BuchungsAssistent")
@app.route(route="buche", auth_level=func.AuthLevel.ANONYMOUS)
async def main(req: func.HttpRequest) -> func.HttpResponse:
    try:
        # Parse request JSON
        try:
            body: Dict[str, Any] = req.get_json()
        except ValueError:
            return func.HttpResponse(
                json.dumps({"error": "Invalid JSON payload"}),
                status_code=400,
                mimetype="application/json",
            )

        user_query: str | None = body.get("userQuery")
        if not user_query:
            return func.HttpResponse(
                json.dumps({"error": "Missing required property: userQuery"}),
                status_code=400,
                mimetype="application/json",
            )
        openAiKey = os.environ["OPENAI_API_KEY"]
        set_tracing_export_api_key(openAiKey)
        agent: Agent = get_or_create_agent()

        # Run agent asynchronously
        result = await Runner.run(agent, user_query)

        response_payload: Dict[str, Any] = {
            "final_output": result.final_output,
            "steps": getattr(result, "steps", None),
        }

        return func.HttpResponse(
            json.dumps(response_payload,ensure_ascii=False, default=str),
            status_code=200,
            mimetype="application/json",
        )

    except Exception as e:
        return func.HttpResponse(
            json.dumps({"error": str(e)}),
            status_code=500,
            mimetype="application/json",
        )
