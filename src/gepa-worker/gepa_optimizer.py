"""
GEPA Optimizer wrapper - handles the actual GEPA optimization logic
"""
import logging
from typing import Dict, List, Any, Callable, Optional

import gepa

logger = logging.getLogger(__name__)


class GepaOptimizer:
	"""Wrapper around GEPA optimization functionality"""

	def __init__(self):
		logger.info("Initialized GEPA Optimizer")

	def optimize(
		self,
		seed_prompt: str,
		trainset: List[Dict[str, Any]],
		valset: List[Dict[str, Any]],
		max_metric_calls: int = 150,
		task_lm: str = "openai/gpt-4o-mini",
		reflection_lm: str = "openai/gpt-4o",
		progress_callback: Optional[Callable[[float], None]] = None
	) -> Dict[str, Any]:
		"""
		Run GEPA optimization

		Args:
			seed_prompt: Initial prompt to optimize
			trainset: Training examples (list of dicts with 'input' and 'output' keys)
			valset: Validation examples
			max_metric_calls: Maximum optimization iterations
			task_lm: LLM to use for task execution
			reflection_lm: LLM to use for reflection/optimization
			progress_callback: Optional callback for progress updates (0.0 to 1.0)

		Returns:
			Dict containing:
				- optimized_prompt: The optimized prompt text
				- metrics: Performance metrics
				- iterations: Number of iterations performed
		"""
		try:
			logger.info(f"Starting GEPA optimization with {len(trainset)} training examples")
			logger.info(f"Task LM: {task_lm}, Reflection LM: {reflection_lm}")
			logger.info(f"Max metric calls: {max_metric_calls}")

			# Convert trainset/valset format if needed
			# GEPA expects specific format depending on task type
			formatted_trainset = self._format_dataset(trainset)
			formatted_valset = self._format_dataset(valset)

			# Progress tracking
			if progress_callback:
				progress_callback(0.2)

			# Run GEPA optimization
			result = gepa.optimize(
				seed_candidate={"system_prompt": seed_prompt},
				trainset=formatted_trainset,
				valset=formatted_valset,
				task_lm=task_lm,
				reflection_lm=reflection_lm,
				max_metric_calls=max_metric_calls,
			)

			if progress_callback:
				progress_callback(0.9)

			# Extract results
			optimized_prompt = result.best_candidate.get('system_prompt', seed_prompt)

			logger.info(f"Optimization complete. Score: {result.best_score}")

			return {
				'optimized_prompt': optimized_prompt,
				'metrics': {
					'score': result.best_score,
					'trainset_score': result.trainset_score,
					'valset_score': result.valset_score
				},
				'iterations': max_metric_calls
			}

		except Exception as e:
			logger.error(f"GEPA optimization failed: {e}", exc_info=True)
			raise

	def _format_dataset(self, dataset: List[Dict[str, Any]]) -> List[Any]:
		"""
		Format dataset for GEPA

		Expected input format (flexible):
		[
			{"input": "question", "output": "answer"},
			{"question": "...", "answer": "..."},
			...
		]

		GEPA format depends on the task - this is a simple example
		You may need to customize this based on your task type
		"""
		if not dataset:
			return []

		formatted = []

		for item in dataset:
			# Try common key patterns
			if 'input' in item and 'output' in item:
				formatted.append({
					'question': item['input'],
					'answer': item['output']
				})
			elif 'question' in item and 'answer' in item:
				formatted.append(item)
			elif 'prompt' in item and 'completion' in item:
				formatted.append({
					'question': item['prompt'],
					'answer': item['completion']
				})
			else:
				# Pass through as-is if format unknown
				formatted.append(item)

		return formatted
