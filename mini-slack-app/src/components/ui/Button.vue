<script setup lang="ts">
import { computed } from 'vue'
import { cn } from '@/lib/utils'

const props = withDefaults(
  defineProps<{
    variant?: 'default' | 'ghost' | 'outline' | 'secondary'
    size?: 'default' | 'sm' | 'icon'
    class?: string
    type?: 'button' | 'submit' | 'reset'
  }>(),
  {
    variant: 'default',
    size: 'default',
    class: '',
    type: 'button',
  },
)

const classes = computed(() =>
  cn(
    'inline-flex shrink-0 items-center justify-center gap-2 rounded-md text-sm font-semibold transition-colors outline-none',
    'focus-visible:ring-2 focus-visible:ring-emerald-600 focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-55',
    props.variant === 'default' && 'bg-emerald-700 text-white hover:bg-emerald-800',
    props.variant === 'secondary' && 'bg-slate-100 text-slate-950 hover:bg-slate-200',
    props.variant === 'outline' &&
      'border border-slate-300 bg-white text-slate-950 hover:bg-slate-50',
    props.variant === 'ghost' && 'text-slate-700 hover:bg-slate-100 hover:text-slate-950',
    props.size === 'default' && 'h-10 px-4',
    props.size === 'sm' && 'h-8 px-3 text-xs',
    props.size === 'icon' && 'h-9 w-9',
    props.class,
  ),
)
</script>

<template>
  <button :type="type" :class="classes">
    <slot />
  </button>
</template>
