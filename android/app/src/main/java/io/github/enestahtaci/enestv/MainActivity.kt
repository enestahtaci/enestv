package io.github.enestahtaci.enestv

import android.annotation.SuppressLint
import android.app.Activity
import android.os.Bundle
import android.os.Process
import android.os.SystemClock
import android.text.Editable
import android.text.TextWatcher
import android.util.Log
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.EditText
import android.widget.TextView
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import io.github.enestahtaci.enestv.core.FoldedIndex

/**
 * Spike: measures cold start, D-pad scrolling and search over a 10,000-channel list.
 * Real sources and playback come in the next steps.
 */
class MainActivity : Activity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        val channels = SyntheticChannels.create(10_000)
        val index = FoldedIndex(channels.asList())
        val adapter = ChannelAdapter(channels)
        val list = findViewById<RecyclerView>(R.id.channels)
        list.layoutManager = LinearLayoutManager(this)
        list.setHasFixedSize(true)
        list.setItemViewCacheSize(8)
        list.adapter = adapter
        list.post { list.getChildAt(0)?.requestFocus() }

        val status = findViewById<TextView>(R.id.status)
        findViewById<EditText>(R.id.search).addTextChangedListener(object : TextWatcher {
            override fun beforeTextChanged(s: CharSequence?, start: Int, count: Int, after: Int) = Unit
            override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) = Unit
            override fun afterTextChanged(s: Editable?) {
                val t0 = System.nanoTime()
                val hits = index.search(s?.toString())
                val searchUs = (System.nanoTime() - t0) / 1000
                adapter.show(hits)
                list.scrollToPosition(0)
                status.text = "${hits.size} sonuç · arama ${"%.1f".format(searchUs / 1000.0)} ms"
                Log.i(PERF_TAG, "search '${s}' hits=${hits.size} in ${searchUs}us")
            }
        })

        window.decorView.post {
            val startMs = SystemClock.uptimeMillis() - Process.getStartUptimeMillis()
            status.text = "${channels.size} kanal · açılış $startMs ms"
            Log.i(PERF_TAG, "first frame after ${startMs}ms, channels=${channels.size}")
            reportFullyDrawn()
        }
    }

    /** Shows the channels at [positions] (all by default); the row number stays the channel's own number. */
    private class ChannelAdapter(private val channels: Array<String>) : RecyclerView.Adapter<ChannelAdapter.Row>() {
        private var positions = IntArray(channels.size) { it }

        class Row(view: View) : RecyclerView.ViewHolder(view) {
            val number: TextView = view.findViewById(R.id.number)
            val name: TextView = view.findViewById(R.id.name)
        }

        @SuppressLint("NotifyDataSetChanged") // a new result set replaces the whole list
        fun show(newPositions: IntArray) {
            positions = newPositions
            notifyDataSetChanged()
        }

        override fun onCreateViewHolder(parent: ViewGroup, viewType: Int) =
            Row(LayoutInflater.from(parent.context).inflate(R.layout.item_channel, parent, false))

        override fun onBindViewHolder(holder: Row, position: Int) {
            val channel = positions[position]
            holder.number.text = (channel + 1).toString()
            holder.name.text = channels[channel]
        }

        override fun getItemCount() = positions.size
    }

    companion object {
        const val PERF_TAG = "enestv.perf"
    }
}
