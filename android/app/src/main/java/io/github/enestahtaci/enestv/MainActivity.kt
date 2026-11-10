package io.github.enestahtaci.enestv

import android.app.Activity
import android.os.Bundle
import android.os.Process
import android.os.SystemClock
import android.util.Log
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView

/**
 * Spike step 1: proves the toolchain and measures the two numbers the UI architecture depends on —
 * cold start to first frame and D-pad scrolling through a 10,000-channel list. Real sources come next.
 */
class MainActivity : Activity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        val channels = SyntheticChannels.create(10_000)
        val list = findViewById<RecyclerView>(R.id.channels)
        list.layoutManager = LinearLayoutManager(this)
        list.setHasFixedSize(true)
        list.setItemViewCacheSize(8)
        list.adapter = ChannelAdapter(channels)
        list.post { list.getChildAt(0)?.requestFocus() }

        val status = findViewById<TextView>(R.id.status)
        window.decorView.post {
            val startMs = SystemClock.uptimeMillis() - Process.getStartUptimeMillis()
            status.text = "${channels.size} kanal · açılış $startMs ms"
            Log.i(PERF_TAG, "first frame after ${startMs}ms, channels=${channels.size}")
            reportFullyDrawn()
        }
    }

    private class ChannelAdapter(private val channels: Array<String>) : RecyclerView.Adapter<ChannelAdapter.Row>() {
        class Row(view: View) : RecyclerView.ViewHolder(view) {
            val number: TextView = view.findViewById(R.id.number)
            val name: TextView = view.findViewById(R.id.name)
        }

        override fun onCreateViewHolder(parent: ViewGroup, viewType: Int) =
            Row(LayoutInflater.from(parent.context).inflate(R.layout.item_channel, parent, false))

        override fun onBindViewHolder(holder: Row, position: Int) {
            holder.number.text = (position + 1).toString()
            holder.name.text = channels[position]
        }

        override fun getItemCount() = channels.size
    }

    companion object {
        const val PERF_TAG = "enestv.perf"
    }
}
